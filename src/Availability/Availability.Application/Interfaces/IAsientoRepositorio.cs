using Availability.Domain.Entities;


namespace Availability.Application.Interfaces
{
    /// <summary>
    /// Acceso a los asientos del catálogo
    ///</summary>
    public interface IAsientoRepositorio
    {
        Task<List<Asiento>> ListarPorEventoAsync(int eventoId);
        Task<Asiento?> ObtenerPorIdAsync(int id);
    }
}
