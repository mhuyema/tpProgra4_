using Availability.Domain.Entities;


namespace Availability.Application.Interfaces
{
    /// <summary>
    /// Acceso a los asientos del catálogo
    ///</summary>
    public interface IAsientoRepositorio
    {
        Task<List<Asiento>> ListarPorEventoAsync(Guid eventoId);
        Task<Asiento?> ObtenerPorIdAsync(Guid id);
    }
}
