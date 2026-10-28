using Availability.Application.Interfaces;
using Availability.Domain.Entities;
using Availability.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace Availability.Infrastructure.Repositorios
{
    /// <summary>Implementación de IEventoRepositorio con EF Core.</summary>
    public class EventoRepositorio(AppDbContext db) : IEventoRepositorio
    {
        public async Task<List<Evento>> ListarEventosAsync() =>
            await db.Eventos.AsNoTracking().OrderBy(e => e.FechaInicio).ToListAsync();


        public async Task<Evento> ObtenerEventoPorIdAsync(Guid id) =>
            await db.Eventos.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
    }
}
