using Availability.Domain.Entities;

namespace Availability.Application.Interfaces
{
    /// <summary>
    /// Acceso a los eventos del catálogo.
    /// </summary>
    public interface IEventoRepositorio
    {
        Task<List<Evento>> ListarEventosAsync();
        Task<Evento> ObtenerEventoPorIdAsync(Guid eventoId);
    }
}
