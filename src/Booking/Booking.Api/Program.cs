using Booking.Api.Handlers;
using Booking.Api.Workers;
using Booking.Business.Services;
using Booking.Data;
using Booking.Data.Repositories;
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

// --- Broker (libreria compartida Broker.Messaging) ---
// AddBroker registra el ServiceBusClient (singleton). La cadena de conexion NO va en el repo:
// se lee de la variable de entorno SERVICEBUS_CONNECTION_STRING.
builder.Services.AddBroker(builder.Configuration);

// Paso 4 del flujo: Booking consume ReservationRequestedMessage desde la suscripcion "booking" del topic.
// Competing Consumers: todas las replicas de Booking usan la MISMA suscripcion, entonces el broker
// entrega cada mensaje a una sola. El handler es Scoped (usa DbContext): la libreria crea un scope por mensaje.
builder.Services.AddScoped<ReservationRequestedHandler>();
builder.Services.AddBrokerSubscriber<ReservationRequestedMessage, ReservationRequestedHandler>("booking");

// Paso 8 del flujo: vence las reservas Retenidas que superaron su tiempo.
builder.Services.AddHostedService<ReservaVencimientoWorker>();

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
