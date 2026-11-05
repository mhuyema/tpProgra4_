using Broker.Messaging.Abstractions;
using Broker.Messaging.Contracts;
using Notifications.Api.Services;

namespace Notifications.Api.Handlers;

// Aviso 2 del punto 3.h: el pago fue exitoso y la reserva quedo Confirmada.
public class ReservationConfirmedHandler : IMessageHandler<ReservationConfirmedMessage>
{
    private readonly INotificationStore _store;
    private readonly INotificationSender _sender;
    private readonly ILogger<ReservationConfirmedHandler> _logger;

    public ReservationConfirmedHandler(INotificationStore store, INotificationSender sender,
        ILogger<ReservationConfirmedHandler> logger)
    {
        _store = store;
        _sender = sender;
        _logger = logger;
    }

    public async Task HandleAsync(ReservationConfirmedMessage message, string correlationId, CancellationToken cancellationToken)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId });

        if (message.RequestId == Guid.Empty || string.IsNullOrWhiteSpace(message.UserId))
        {
            _logger.LogError("[AUDITORIA] Aviso de reserva confirmada incompleto, se descarta. RequestId={RequestId} UserId={UserId}",
                message.RequestId, message.UserId);
            return;
        }

        var texto = $"Tu reserva del asiento {message.SeatId} del evento {message.EventId} fue confirmada. Pago recibido: {message.Amount:0.00}.";

        var notificacion = new Notification(message.RequestId, "ReservaConfirmada", message.UserId, texto, DateTime.UtcNow);

        if (!_store.TryAdd(notificacion))
        {
            _logger.LogWarning("Aviso duplicado, ya se informo la reserva confirmada de la solicitud {RequestId}", message.RequestId);
            return;
        }

        await _sender.SendAsync(notificacion);
    }
}
