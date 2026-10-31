using Booking.Api.Handlers;
using Booking.Api.Workers;
using Booking.Business.Services;
using Booking.Data;
using Booking.Data.Repositories;
using Payment.Contracts;
using ProtoBuf.Grpc.ClientFactory;
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

// --- Broker (libreria compartida Broker.Messaging) ---
// AddBroker registra el ServiceBusClient (singleton). La cadena de conexion NO va en el repo:
// se lee de la variable de entorno SERVICEBUS_CONNECTION_STRING.
builder.Services.AddBroker(builder.Configuration);

// Booking también PUBLICA en el topic (avisos de reserva creada y confirmada para Notificaciones, 3.h).
// Business solo conoce el contrato INotificadorReservas; NotificadorBroker lo implementa con el publisher compartido.
builder.Services.AddBrokerPublisher();
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