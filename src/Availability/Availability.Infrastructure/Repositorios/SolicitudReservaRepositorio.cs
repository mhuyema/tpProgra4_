using Availability.Application.Interfaces;
using Availability.Domain.Entities;
using Availability.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Availability.Infrastructure.Repositorios
{
    /// <summary>Implementación de ISolicitudReservaRepositorio con EF Core.</summary>
    public class SolicitudReservaRepositorio(AppDbContext db) : ISolicitudReservaRepositorio
    {
        public async Task GuardarAsync(SolicitudReserva solicitud)
        {
            db.Solicitudes.Add(solicitud);
            await db.SaveChangesAsync();
        }

        public async Task<SolicitudReserva?> ObtenerPorIdAsync(Guid id) =>
            await db.Solicitudes.AsNoTracking().FirstOrDefaultAsync(s => s.RequestId == id);
    }
}
