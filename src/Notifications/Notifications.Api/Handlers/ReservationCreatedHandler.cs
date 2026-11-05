using Broker.Messaging.Abstractions;
using Broker.Messaging.Contracts;
using Notifications.Api.Auditing;
using Notifications.Api.Services;

namespace Notifications.Api.Handlers;

// Aviso 1 del punto 3.h: la reserva quedó creada y el asiento está apartado hasta ExpiresAt.
//
// ¿Qué es un "handler"? Es la clase que sabe qué hacer cuando llega un mensaje del broker.
// El suscriptor compartido (ServiceBusSubscriberService) recibe el mensaje, lo convierte a
// ReservationCreatedMessage y llama a HandleAsync. Si HandleAsync termina bien, el mensaje se
// confirma; si lanza una excepción, el broker lo reentrega más tarde (hasta un máximo de intentos).
public class ReservationCreatedHandler : IMessageHandler<ReservationCreatedMessage>
{
    // Nombre del proceso tal como va a aparecer en el servicio AuditLog.
    private const string ProcessName = "NotificarReservaCreada";

    private readonly INotificationStore _store;
    private readonly INotificationSender _sender;
    private readonly INotificationAuditPublisher _audit;
    private readonly ILogger<ReservationCreatedHandler> _logger;

    // Inyección de dependencias: no hacemos "new" de nada acá. Program.cs le dice a .NET qué clase
    // concreta usar para cada interfaz, y .NET las pasa solas al constructor.
    public ReservationCreatedHandler(INotificationStore store, INotificationSender sender,
        INotificationAuditPublisher audit, ILogger<ReservationCreatedHandler> logger)
    {
        _store = store;
        _sender = sender;
        _audit = audit;
        _logger = logger;
    }

    public async Task HandleAsync(ReservationCreatedMessage message, string correlationId, CancellationToken cancellationToken)
    {
        // BeginScope hace que todos los logs de este bloque lleven el CorrelationId automáticamente.
        using var logScope = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId });

        // Mensaje inválido: reintentarlo no lo arregla (le falta un dato). Se registra el error y se
        // descarta; retornar sin lanzar excepción hace que el broker lo dé por procesado.
        if (message.RequestId == Guid.Empty || string.IsNullOrWhiteSpace(message.UserId))
        {
            _logger.LogError("[AUDITORIA] Aviso de reserva creada incompleto, se descarta. RequestId={RequestId} UserId={UserId}",
                message.RequestId, message.UserId);
            await _audit.EmitErrorAsync(correlationId, ProcessName,
                new ArgumentException("El aviso de reserva creada llegó sin RequestId o sin UserId."),
                new Dictionary<string, object?> { ["RequestId"] = message.RequestId, ["UserId"] = message.UserId },
                cancellationToken);
            return;
        }

        var detalles = new Dictionary<string, object?>
        {
            ["RequestId"] = message.RequestId,
            ["UserId"] = message.UserId,
            ["EventId"] = message.EventId,
            ["SeatId"] = message.SeatId
        };

        // Inicio de la cadena de sucesos de este microservicio (punto 3.f).
        await _audit.InitProcessAsync(correlationId, ProcessName, detalles, cancellationToken);

        try
        {
            var vence = message.ExpiresAt is { } fin
                ? $" Tenes hasta las {fin.ToLocalTime():HH:mm} para completar el pago."
                : string.Empty;
            var texto = $"Tu reserva del asiento {message.SeatId} del evento {message.EventId} fue creada.{vence}";

            var notificacion = new Notification(message.RequestId, "ReservaCreada", message.UserId, texto, DateTime.UtcNow);

            // Idempotencia: el broker entrega "al menos una vez", o sea que el mismo mensaje puede llegar
            // dos veces. Si ya registramos este aviso (misma solicitud + mismo tipo), no lo mandamos de nuevo.
            // Se registra antes de enviar. Con el canal simulado no hay riesgo de que el envío falle; con un
            // proveedor real (email, SMS) habría que decidir entre "al menos una vez" (enviar y después
            // registrar) o "como máximo una vez" (registrar y después enviar, como está acá).
            if (!_store.TryAdd(notificacion))
            {
                _logger.LogWarning("Aviso duplicado, ya se informo la reserva creada de la solicitud {RequestId}", message.RequestId);
                await _audit.EndProcessAsync(correlationId, ProcessName, "Duplicada", detalles, cancellationToken);
                return;
            }

            await _sender.SendAsync(notificacion);

            // Fin de la cadena de sucesos.
            await _audit.EndProcessAsync(correlationId, ProcessName, "Enviada", detalles, cancellationToken);
        }
        catch (Exception ex)
        {
            // Dejamos el error en el AuditLog y volvemos a lanzarlo ("throw;") para que el suscriptor
            // abandone el mensaje y el broker lo reintente. Sin el throw, el error quedaría tapado.
            await _audit.EmitErrorAsync(correlationId, ProcessName, ex, detalles, cancellationToken);
            throw;
        }
    }
}
