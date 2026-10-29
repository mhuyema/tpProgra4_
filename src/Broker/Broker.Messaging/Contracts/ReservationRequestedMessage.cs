namespace Broker.Messaging.Contracts;

/// <summary>
/// Mensaje que Availability publica al recibir una solicitud de reserva y que Booking consume.
/// Es el contrato compartido entre ambos servicios. Es un mensaje anémico (solo datos), igual que el dominio.
/// </summary>
/// <param name="RequestId">Identificador único de la solicitud (también viaja como correlation id y MessageId).</param>
public record ReservationRequestedMessage(Guid RequestId, int EventId, int SeatId, string UserId);
