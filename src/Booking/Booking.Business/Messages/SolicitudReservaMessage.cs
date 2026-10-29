namespace Booking.Business.Messages;

// Se actualizaron los nombres de las propiedades para coincidir
// con el contrato acordado entre Availability, Broker y Booking.
public record SolicitudReservaMessage(
    Guid RequestId,
    int EventId,
    int SeatId,
    string UserId
);