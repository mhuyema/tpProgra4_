using Broker.Messaging.Contracts;
using Broker.Messaging.DependencyInjection;
using Notifications.Api.Auditing;
using Notifications.Api.Auth;
using Notifications.Api.Endpoints;
using Notifications.Api.Handlers;
using Notifications.Api.Services;

// Microservicio de Notificaciones (punto 3.h). No tiene dominio propio: consume los avisos que publica
// Booking (reserva creada / reserva confirmada) y le informa al usuario. Por eso es un solo proyecto,
// sin las capas de Booking: no hay reglas de negocio que separar.
//
// Program.cs tiene dos partes: (1) REGISTRAR servicios en el contenedor de inyección de dependencias
// (todo lo que va antes de builder.Build()) y (2) CONFIGURAR qué hace la app al recibir pedidos
// (lo que va después). Es el "tablero de conexiones" del servicio.
var builder = WebApplication.CreateBuilder(args);

// Broker (librería compartida Broker.Messaging). La cadena de conexión NO va en el repo:
// se lee de Broker:ConnectionString (User Secrets) o, si no está, de la variable de entorno
// SERVICEBUS_CONNECTION_STRING (la que usa el docker-compose).
builder.Services.AddBroker(builder.Configuration);

// Publisher del broker: antes Notifications solo CONSUMÍA mensajes. Ahora también PUBLICA, pero únicamente
// sus mensajes de auditoría (AuditLogMessage) para el servicio AuditLog (punto 3.f).
builder.Services.AddBrokerPublisher();
builder.Services.AddScoped<INotificationAuditPublisher, NotificationAuditPublisher>();

// Estado en memoria y "canal" de envío. Son singleton (una sola instancia para toda la app) porque los
// handlers se crean uno por mensaje y el registro de avisos tiene que sobrevivir entre mensajes.
// Se reemplazan por una base de datos / un proveedor real de email o SMS sin tocar los handlers.
builder.Services.AddSingleton<INotificationStore, InMemoryNotificationStore>();

// Si hay un servidor SMTP configurado (Smtp:Host) se mandan mails reales; si no, se sigue simulando en el log.
// Asi el servicio arranca igual en una maquina sin SMTP.
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
if (string.IsNullOrWhiteSpace(builder.Configuration["Smtp:Host"]))
    builder.Services.AddSingleton<INotificationSender, SimulatedNotificationSender>();
else
    builder.Services.AddSingleton<INotificationSender, EmailNotificationSender>();


// Autenticación del GET de consulta (punto 3.e): el token que manda el usuario se valida contra el
// Authorization Server. AddMemoryCache es el caché en memoria que usa el validador para no repetir consultas.
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IValidadorTokenUsuario, ValidadorTokenUsuarioHttp>(client =>
{
    // Si falta la configuración, fallamos al arrancar con un mensaje claro (en vez de fallar
    // recién en el primer pedido con un error confuso).
    var authority = builder.Configuration["Auth:Authority"]
        ?? throw new InvalidOperationException("No se configuró Auth:Authority (URL del Authorization Server).");

    client.BaseAddress = new Uri(authority.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Un handler por tipo de aviso, y cada uno con su PROPIA suscripción del topic.
// Si los dos compartieran suscripción, un mensaje de un tipo podría caer en el procesador del otro, que
// lo ignora por su Subject y se perdería. Con una suscripción por tipo, cada copia llega a quien la procesa.
// Las dos suscripciones tienen que existir en Azure con estos nombres exactos.
builder.Services.AddScoped<ReservationCreatedHandler>();
builder.Services.AddScoped<ReservationConfirmedHandler>();
builder.Services.AddBrokerSubscriber<ReservationCreatedMessage, ReservationCreatedHandler>("notificaciones-creada");
builder.Services.AddBrokerSubscriber<ReservationConfirmedMessage, ReservationConfirmedHandler>("notificaciones-confirmada");

var app = builder.Build();

// Consulta de los avisos enviados a un usuario. Ahora exige token: cada usuario ve solo los suyos.
// Availability usa este endpoint para mostrar los avisos en la pantalla de seguimiento de la reserva.
app.MapNotificacionesEndpoints();

app.Run();
