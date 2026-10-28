using Booking.Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Booking.Data.Repositories;

// Repository: aisla el acceso a datos. Business no conoce EF core ni SQL Server
public class ReservaRepository : IReservaRepository
{
    private readonly BookingDbContext _db;
    public ReservaRepository(BookingDbContext db) => _db = db;

    public Task<Reserva?> ObtenerPorSolicitudAsync(Guid solicitudId) =>
        _db.Reservas.AsNoTracking().FirstOrDefaultAsync(r => r.SolicitudId == solicitudId);

    public async Task AgregarAsync (Reserva reserva)
    {
        _db.Reservas.Add(reserva);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> TryAgregarRetenidaAsync(Reserva reserva)
    {
        try
        {
            await AgregarAsync(reserva);
            return true;
        }
        // 2601 / 2627 = violacion de indice unico: otra reserva activa ya tiene ese asiento
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627})
        {
            _db.ChangeTracker.Clear(); // Descarta la entidad fallida para poder seguir usando el contexto
            return false;
        }
    }
}