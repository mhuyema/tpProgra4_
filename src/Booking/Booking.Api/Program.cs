using Booking.Api.Handlers;
using Booking.Api.Workers;
using Booking.Api.Auditing;
using Booking.Business.Auditing;
using Booking.Business.Services;
using Booking.Data;
using Booking.Data.Repositories;
using Payment.Contracts;
using ProtoBuf.Grpc.ClientFactory;
using Booking.Business.Auth;
using Booking.Business.Payments;
using Booking.Business.Notifications;
using Booking.API.Integrations;
using Broker.Messaging.Contracts;
using Broker.Messaging.DependencyInjection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConexion")));
builder.Services.AddScoped<IReservaService, ReservaService>();
builder.Services.AddScoped<IReservaRepository, ReservaRepository>();

// Registra el cliente gRPC de Payment utilizando la dirección
// configurada para el entorno de ejecución.
builder.Services.AddCodeFirstGrpcClient<IPaymentGrpcService>(options =>
{
    var paymentUrl = builder.Configuration["Payment:GrpcUrl"]
        ?? throw new InvalidOperationException(
            "No se configuró la dirección gRPC de Payment.");

    options.Address = new Uri(paymentUrl);
});

// Vincula el contrato interno de pagos con su implementación gRPC,
// manteniendo desacoplada la capa Business de la comunicación externa.
builder.Services.AddScoped<IPagoGateway, PagoGrpcGateway>();

// Validación delegada de identidad: Booking verifica el access token de cada solicitud llamando
// a GET /api/auth/validate del Authorization Server (Auth:Authority). Business solo conoce IValidadorToken.
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IValidadorToken, ValidadorTokenHttp>(client =>
{
    var authority = builder.Configuration["Auth:Authority"]
        ?? throw new InvalidOperationException("No se configuró Auth:Authority (URL del Authorization Server).");

    client.BaseAddress = new Uri(authority.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Token de SERVICIO para llamar a Payment: Booking lo pide a /connect/token del Authorization Server (client credentials)
// y PagoGrpcGateway lo adjunta en la metadata gRPC. Payment lo valida de forma delegada contra el mismo servidor.
builder.Services.AddHttpClient<IProveedorTokenServicio, ProveedorTokenServicio>(client =>
{
    var authority = builder.Configuration["Auth:Authority"]
        ?? throw new InvalidOperationException("No se configuró Auth:Authority (URL del Authorization Server).");

    client.BaseAddress = new Uri(authority.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// --- Broker (libreria compartida Broker.Messaging) ---
// AddBroker registra el ServiceBusClient (singleton). La cadena de conexion NO va en el repo:
// se lee de la variable de entorno SERVICEBUS_CONNECTION_STRING.
builder.Services.AddBroker(builder.Configuration);

// Booking también PUBLICA en el topic (avisos de reserva creada y confirmada para Notificaciones, 3.h).
// Business solo conoce el contrato INotificadorReservas; NotificadorBroker lo implementa con el publisher compartido.
builder.Services.AddBrokerPublisher();
// Auditoría estructurada al broker (AuditLogMessage), mismo patrón que Payment. Nunca interrumpe el flujo.
builder.Services.AddScoped<IBookingAuditPublisher, BookingAuditPublisher>();
builder.Services.AddScoped<INotificadorReservas, NotificadorBroker>();

// Paso 4 del flujo: Booking consume ReservationRequestedMessage desde la suscripcion "booking" del topic.
// Competing Consumers: todas las replicas de Booking usan la MISMA suscripcion, entonces el broker
// entrega cada mensaje a una sola. El handler es Scoped (usa DbContext): la libreria crea un scope por mensaje.
builder.Services.AddScoped<ReservationRequestedHandler>();
builder.Services.AddBrokerSubscriber<ReservationRequestedMessage, ReservationRequestedHandler>("booking");

// Paso 8 del flujo: vence las reservas Retenidas que superaron su tiempo.
builder.Services.AddHostedService<ReservaVencimientoWorker>();

// Recupera pagos retenidos que siguen en procesamiento o cuya
// comunicación con Payment falló, sin crear nuevos intentos.
builder.Services.AddHostedService<PagosPendientesWorker>();

var app = builder.Build();

// Aplica las migraciones al arrancar para que el primer mensaje no falle por falta de tablas.
// Para el TP alcanza; en produccion se migraria como paso aparte del deploy (con varias replicas
// arrancando a la vez podrian competir).
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<BookingDbContext>().Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();