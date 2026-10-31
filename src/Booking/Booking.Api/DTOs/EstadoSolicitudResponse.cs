namespace Booking.API.DTOs;

// Datos que Booking devuelve a Availability al consultar una solicitud.
public record EstadoSolicitudResponse(
    Guid RequestId,
    string Status,
    string? RejectionReason,
    DateTime? ExpiresAt
);