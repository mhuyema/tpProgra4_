namespace Broker.Messaging.Contracts;

/// <summary>
/// Aviso que Booking publica cuando el pago fue exitoso y la reserva quedó Confirmada.
/// Lo consume el servicio de Notificaciones para informar al usuario (punto 3.h).
/// Es un mensaje anémico (solo datos), igual que el dominio.
/// </summary>
/// <param name="RequestId">Identificador de la solicitud original (también viaja como correlation id).</param>
/// <param name="ReservationId">Identificador de la reserva en Booking.</param>
/// <param name="EventId">Identificador del evento reservado.</param>
/// <param name="SeatId">Identificador del asiento reservado.</param>
/// <param name="UserId">Identificador del usuario a notificar.</param>
/// <param name="Amount">Importe cobrado.</param>
public record ReservationConfirmedMessage(
    Guid RequestId,
    Guid ReservationId,
    int EventId,
    int SeatId,
    string UserId,
    decimal Amount
);
