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

    public async Task AgregarAsync(Reserva reserva)
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
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            _db.ChangeTracker.Clear(); // Descarta la entidad fallida para poder seguir usando el contexto
            return false;
        }
    }

    // Actualiza el estado directamente en SQL mediante una operación atómica.
    // Impide confirmar una reserva cuyo tiempo de retención ya terminó.
    // Aplica el vencimiento también a los rechazos y permite marcar
    // como Vencida una reserva cuando su plazo se ha agotado.
    public async Task<bool> TryActualizarEstadoAsync(
        Guid reservaId,
        EstadoReserva estadoEsperado,
        EstadoReserva estadoNuevo,
        MotivoRechazo? motivo)
    {
        var ahora = DateTime.UtcNow;

        var filasActualizadas = await _db.Reservas
            .Where(r =>
                r.Id == reservaId &&
                r.Estado == estadoEsperado &&
                r.VenceEn != null &&
                (estadoNuevo == EstadoReserva.Vencida
                    ? r.VenceEn <= ahora
                    : r.VenceEn > ahora))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Estado, estadoNuevo)
                .SetProperty(r => r.Motivo, motivo));

        return filasActualizadas == 1;
    }

    // Registra el intento de pago mediante una actualización atómica.
    // Evita que dos procesos asignen identificadores diferentes a la misma reserva.
    public async Task<bool> TryAsignarIntentoPagoAsync(
        Guid reservaId,
        Guid intentoPagoId)
    {
        var ahora = DateTime.UtcNow;

        var filasActualizadas = await _db.Reservas
            .Where(r =>
                r.Id == reservaId &&
                r.Estado == EstadoReserva.Retenida &&
                r.VenceEn != null &&
                r.VenceEn > ahora &&
                r.IntentoPagoId == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.IntentoPagoId, (Guid?)intentoPagoId));

        return filasActualizadas == 1;
    }

    // UPDATE ... WHERE Id = @id AND Estado = @desde: atomico en la base, sin leer-modificar-guardar.
    // Evita que Booking confirme una reserva que el worker de vencimiento ya vencio (y viceversa)
    public Task<bool> TransicionarAsync(
        Guid reservaId,
        EstadoReserva desde,
        EstadoReserva hacia,
        MotivoRechazo? motivo = null)
    {
        // Reutiliza las mismas comprobaciones de estado y vencimiento
        // utilizadas por la integración con Payment.
        return TryActualizarEstadoAsync(reservaId, desde, hacia, motivo);
    }

    public Task<int> VencerRetenidasAsync(DateTime ahoraUtc) =>
        _db.Reservas
            .Where(r => r.Estado == EstadoReserva.Retenida &&
                        r.VenceEn != null &&
                        r.VenceEn < ahoraUtc)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(r => r.Estado, EstadoReserva.Vencida));
}