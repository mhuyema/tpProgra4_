using Broker.Messaging.Abstractions;
using Broker.Messaging.Contracts;
using Notifications.Api.Auditing;
using Notifications.Api.Services;

namespace Notifications.Api.Handlers;

// Aviso 2 del punto 3.h: el pago fue exitoso y la reserva quedó Confirmada.
// Es igual que ReservationCreatedHandler pero para otro tipo de mensaje. Se mantienen separados (en vez de
// un handler genérico) porque cada uno arma un texto distinto y tiene su propia suscripción del topic.
public class ReservationConfirmedHandler : IMessageHandler<ReservationConfirmedMessage>
{
    private const string ProcessName = "NotificarReservaConfirmada";

    private readonly INotificationStore _store;
    private readonly INotificationSender _sender;
    private readonly INotificationAuditPublisher _audit;
    private readonly ILogger<ReservationConfirmedHandler> _logger;

    public ReservationConfirmedHandler(INotificationStore store, INotificationSender sender,
        INotificationAuditPublisher audit, ILogger<ReservationConfirmedHandler> logger)
    {
        _store = store;
        _sender = sender;
        _audit = audit;
        _logger = logger;
    }

    public async Task HandleAsync(ReservationConfirmedMessage message, string correlationId, CancellationToken cancellationToken)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId });

        // Mensaje inválido: se registra el error y se descarta (reintentar no lo arregla).
        if (message.RequestId == Guid.Empty || string.IsNullOrWhiteSpace(message.UserId))
        {
            _logger.LogError("[AUDITORIA] Aviso de reserva confirmada incompleto, se descarta. RequestId={RequestId} UserId={UserId}",
                message.RequestId, message.UserId);
            await _audit.EmitErrorAsync(correlationId, ProcessName,
                new ArgumentException("El aviso de reserva confirmada llegó sin RequestId o sin UserId."),
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
            var texto = $"Tu reserva del asiento {message.SeatId} del evento {message.EventId} fue confirmada. Pago recibido: {message.Amount:0.00}.";

            var notificacion = new Notification(message.RequestId, "ReservaConfirmada", message.UserId, texto, DateTime.UtcNow);

            // Idempotencia (ver el comentario de ReservationCreatedHandler): misma solicitud + mismo tipo = un solo aviso.
            if (!_store.TryAdd(notificacion))
            {
                _logger.LogWarning("Aviso duplicado, ya se informo la reserva confirmada de la solicitud {RequestId}", message.RequestId);
                await _audit.EndProcessAsync(correlationId, ProcessName, "Duplicada", detalles, cancellationToken);
                return;
            }

            await _sender.SendAsync(notificacion);

            // Fin de la cadena de sucesos.
            await _audit.EndProcessAsync(correlationId, ProcessName, "Enviada", detalles, cancellationToken);
        }
        catch (Exception ex)
        {
            await _audit.EmitErrorAsync(correlationId, ProcessName, ex, detalles, cancellationToken);
            throw;
        }
    }
}

