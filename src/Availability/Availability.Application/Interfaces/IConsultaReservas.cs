using Availability.Application.DTOs;

namespace Availability.Application.Interfaces
{
    /// <summary>
    /// Consultas a Booking, dueño de la ocupación y el estado de las reservas.
    /// </summary>
    public interface IConsultaReservas
    {
        Task<List<Guid>> ObtenerAsientosOcupadosAsync(Guid eventoId);
        Task<EstadoSolicitudDTO> ObtenerEstadoAsync(Guid solicitudId);
    }
    }
