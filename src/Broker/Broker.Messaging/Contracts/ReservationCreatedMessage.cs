namespace Broker.Messaging.Contracts;

/// <summary>
/// Aviso que Booking publica cuando la reserva quedó creada (Retenida) y el asiento está apartado.
/// Lo consume el servicio de Notificaciones para informar al usuario (punto 3.h).
/// Es un mensaje anémico (solo datos), igual que el dominio.
/// </summary>
/// <param name="RequestId">Identificador de la solicitud original (también viaja como correlation id).</param>
/// <param name="ReservationId">Identificador de la reserva en Booking.</param>
/// <param name="EventId">Identificador del evento reservado.</param>
/// <param name="SeatId">Identificador del asiento reservado.</param>
/// <param name="UserId">Identificador del usuario a notificar.</param>
/// <param name="Amount">Importe de la reserva.</param>
/// <param name="ExpiresAt">Fin de la retención (UTC): si no se confirma el pago antes, la reserva vence.</param>
public record ReservationCreatedMessage(
    Guid RequestId,
    Guid ReservationId,
    int EventId,
    int SeatId,
    string UserId,
    decimal Amount,
    DateTime? ExpiresAt
);
