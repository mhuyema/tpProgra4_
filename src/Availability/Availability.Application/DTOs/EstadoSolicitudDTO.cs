

namespace Availability.Application.DTOs
{
    /// <summary>Lo que devuelve ConsultarEstadoSolicitud 
    /// (y la consulta a Booking).</summary>
    public record EstadoSolicitudDTO(
        Guid SolicitudId,
        string Estado,
        string? Motivo,
        decimal Monto = 0);
}
