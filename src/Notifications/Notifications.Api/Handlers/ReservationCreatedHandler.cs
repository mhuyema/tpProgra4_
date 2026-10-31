using Broker.Messaging.Abstractions;
using Broker.Messaging.Contracts;
using Notifications.Api.Services;

namespace Notifications.Api.Handlers;

// Aviso 1 del punto 3.h: la reserva quedo creada y el asiento esta apartado hasta ExpiresAt.
// Si HandleAsync lanza una excepcion, el suscriptor abandona el mensaje y el broker lo reentrega.
public class ReservationCreatedHandler : IMessageHandler<ReservationCreatedMessage>
{
    private readonly INotificationStore _store;
    private readonly INotificationSender _sender;
    private readonly ILogger<ReservationCreatedHandler> _logger;

    public ReservationCreatedHandler(INotificationStore store, INotificationSender sender,
        ILogger<ReservationCreatedHandler> logger)
    {
        _store = store;
        _sender = sender;
        _logger = logger;
    }

    public async Task HandleAsync(ReservationCreatedMessage message, string correlationId, CancellationToken cancellationToken)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId });

        // Mensaje invalido: reintentarlo no lo arregla. Se registra y se descarta (retornar lo confirma).
        if (message.RequestId == Guid.Empty || string.IsNullOrWhiteSpace(message.UserId))
        {
            _logger.LogError("[AUDITORIA] Aviso de reserva creada incompleto, se descarta. RequestId={RequestId} UserId={UserId}",
                message.RequestId, message.UserId);
            return;
        }

        var vence = message.ExpiresAt is { } fin ? $" Tenes hasta las {fin.ToLocalTime():HH:mm} para completar el pago." : string.Empty;
        var texto = $"Tu reserva del asiento {message.SeatId} del evento {message.EventId} fue creada.{vence}";

        var notificacion = new Notification(message.RequestId, "ReservaCreada", message.UserId, texto, DateTime.UtcNow);

        // Idempotencia: se registra ANTES de enviar. Con el canal simulado no hay riesgo de que el envio falle;
        // con un proveedor real habria que decidir entre "al menos una vez" (enviar y despues registrar).
        if (!_store.TryAdd(notificacion))
        {
            _logger.LogWarning("Aviso duplicado, ya se informo la reserva creada de la solicitud {RequestId}", message.RequestId);
            return;
        }

        await _sender.SendAsync(notificacion);
    }
}
