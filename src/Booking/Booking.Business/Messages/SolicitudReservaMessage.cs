namespace Booking.Business.Messages;

// Contrato del mensaje que Availability publica en el broker.
public record SolicitudReservaMessage(Guid SolicitudId, int EventoId, int AsientoId, string UsuarioId);