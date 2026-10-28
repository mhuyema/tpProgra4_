using Availability.Application.Interfaces;
using Availability.Domain.Entities;
using Availability.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Availability.Infrastructure.Repositorios
{
    /// <summary>Implementación de IAsientoRepositorio con EF Core.</summary>
    public class AsientoRepositorio(AppDbContext db) : IAsientoRepositorio
    {
        public async Task<List<Asiento>> ListarPorEventoAsync(Guid eventoId) =>
            await db.Asientos.AsNoTracking()
                .Where(a => a.EventoId == eventoId)
                .OrderBy(a => a.Fila).ThenBy(a => a.Numero)
                .ToListAsync();

        public async Task<Asiento?> ObtenerPorIdAsync(Guid id) =>
            await db.Asientos.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
    }
}
