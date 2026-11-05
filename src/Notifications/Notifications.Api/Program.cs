using Broker.Messaging.Contracts;
using Broker.Messaging.DependencyInjection;
using Notifications.Api.Handlers;
using Notifications.Api.Services;

// Microservicio de Notificaciones (punto 3.h). No tiene dominio propio: consume los avisos que publica
// Booking (reserva creada / reserva confirmada) y le informa al usuario. Por eso es un solo proyecto,
// sin las capas de Booking: no hay reglas de negocio que separar.
var builder = WebApplication.CreateBuilder(args);

// Broker (libreria compartida Broker.Messaging). La cadena de conexion NO va en el repo:
// se lee de Broker:ConnectionString (User Secrets) o, si no esta la variable de entorno
// SERVICEBUS_CONNECTION_STRING (la que usa el docker-compose)
builder.Services.AddBroker(builder.Configuration);

// Estado en memoria y "canal" de envio. Son singleton porque los handlers se crean uno por mensaje.
// Se reemplazan por una base de datos / un proveedor real de email o SMS sin tocar los handlers.
builder.Services.AddSingleton<INotificationStore, InMemoryNotificationStore>();

// Si hay un servidor SMTP configurado (Smtp:Host) se mandan mails reales; si no, se sigue simulando en el log.
// Asi el servicio arranca igual en una maquina sin SMTP.
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
if (string.IsNullOrWhiteSpace(builder.Configuration["Smtp:Host"]))
    builder.Services.AddSingleton<INotificationSender, SimulatedNotificationSender>();
else
    builder.Services.AddSingleton<INotificationSender, EmailNotificationSender>();


// Un handler por tipo de aviso, y cada uno con su PROPIA suscripcion del topic.
// Si los dos compartieran suscripcion, un mensaje de un tipo podria caer en el procesador del otro, que
// lo ignora por su Subject y se perderia. Con una suscripcion por tipo, cada copia llega a quien la procesa.
builder.Services.AddScoped<ReservationCreatedHandler>();
builder.Services.AddScoped<ReservationConfirmedHandler>();
builder.Services.AddBrokerSubscriber<ReservationCreatedMessage, ReservationCreatedHandler>("notificaciones-creada");
builder.Services.AddBrokerSubscriber<ReservationConfirmedMessage, ReservationConfirmedHandler>("notificaciones-confirmada");

var app = builder.Build();

// Consulta de los avisos enviados a un usuario (sirve para mostrar y verificar el servicio).
app.MapGet("/api/notificaciones/{userId}", (string userId, INotificationStore store) =>
    Results.Ok(store.GetByUser(userId)));

app.Run();
