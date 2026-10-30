namespace Booking.Business.Messages;

// Contrato de solicitud de reserva entre Availability y Booking.
// Amount representa el importe determinado al iniciar la solicitud.
public record SolicitudReservaMessage(
    Guid RequestId,
    int EventId,
    int SeatId,
    string UserId,
    decimal Amount
);