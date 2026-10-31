using Availability.Application.DTOs;

namespace Availability.Application.Interfaces
{
    /// <summary>
    /// Consultas a Booking, dueño de la ocupación y el estado de las reservas.
    /// </summary>
    public interface IConsultaReservas
    {
        Task<List<int>> ObtenerAsientosOcupadosAsync(int eventoId);
        Task<EstadoSolicitudDTO> ObtenerEstadoAsync(Guid solicitudId);
    }
}
