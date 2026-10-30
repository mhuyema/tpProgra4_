namespace Broker.Messaging.Contracts;

/// <summary>
/// Mensaje que Availability publica al recibir una solicitud de reserva y que Booking consume.
/// Es el contrato compartido entre ambos servicios. Es un mensaje anémico (solo datos), igual que el dominio.
/// </summary>
/// <param name="RequestId">Identificador único de la solicitud (también viaja como correlation id y MessageId).</param>
/// <param name="EventId">Identificador del evento solicitado.</param>
/// <param name="SeatId">Identificador del asiento solicitado.</param>
/// <param name="UserId">Identificador del usuario que solicita la reserva.</param>
/// <param name="Amount">Importe de la reserva determinado por Availability.</param>
public record ReservationRequestedMessage(
    Guid RequestId,
    int EventId,
    int SeatId,
    string UserId,
    decimal Amount
);