namespace Broker.Messaging.Contracts;

// Al incorporar Amount al contrato compartido entre Availability y Booking,
// se completó la documentación XML de todos los parámetros del mensaje.
// Originalmente solo estaba documentado RequestId, lo que generaba
// advertencias CS1573 por los parámetros sin documentar.

/// <summary>
/// Mensaje que Availability publica al recibir una solicitud de reserva y que Booking consume.
/// Es el contrato compartido entre ambos servicios. Es un mensaje anémico (solo datos), igual que el dominio.
/// </summary>
/// <param name="RequestId">Identificador único de la solicitud (también viaja como correlation id).</param>
/// <param name="EventId">Identificador del evento solicitado.</param>
/// <param name="SeatId">Identificador del asiento solicitado.</param>
/// <param name="UserId">Identificador del usuario que solicita la reserva.</param>
/// <param name="Amount">Importe de la reserva determinado por Availability.</param>
/// <param name="AccessToken">
/// Access token del usuario emitido por el Authorization Server. Booking lo valida contra
/// GET /api/auth/validate y exige que su subject coincida con UserId; si falta o no es válido,
/// rechaza la solicitud. Es un secreto: no se loguea. Vence a los 15 minutos.
/// </param>
public record ReservationRequestedMessage(
    Guid RequestId,
    int EventId,
    int SeatId,
    string UserId,
    decimal Amount,
    string? AccessToken = null
);