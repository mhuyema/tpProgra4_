using Booking.Business.Messages;
using Booking.Data.Entities;
using Booking.Data.Repositories;

namespace Booking.Business.Services;

// Dominio anemico: las reglas viven aca, no en la entidad Reserva
public class  ReservaService : IReservaService
{
    private static readonly TimeSpan TiempoRetencion = TimeSpan.FromMinutes(5);
    private readonly IReservaRepository _repo;

    public ReservaService(IReservaRepository repo) => _repo = repo;

    public async Task<Reserva> ProcesarSolicitudAsync(SolicitudReservaMessage m)
    {
        // Idempotencia: si el broker entrega el mensaje dos veces, devolvemos la misma reserva
        var existente = await _repo.ObtenerPorSolicitudAsync(m.SolicitudId);
        if (existente is not null) return existente;

        var ahora = DateTime.UtcNow;
        var reserva = new Reserva
        {
            Id = Guid.NewGuid(),
            SolicitudId = m.SolicitudId,
            EventoId = m.EventoId,
            AsientoId = m.AsientoId,
            UsuarioId = m.UsuarioId,
            Estado = EstadoReserva.Retenida,
            CreadaEn = ahora,
            VenceEn = ahora + TiempoRetencion
        };

        // La base decide quien gana si dos replicas compiten por el mismo asiento
        if (await _repo.TryAgregarRetenidaAsync(reserva)) return reserva;

        // Fallo: puede ser asiento ocupado, o el mismo mensaje procesado en parelelo
        existente = await _repo.ObtenerPorSolicitudAsync(m.SolicitudId);
        if (existente is not null) return existente;

        var rechazada = new Reserva
        {
            Id = Guid.NewGuid(),
            SolicitudId = m.SolicitudId,
            EventoId = m.EventoId,
            AsientoId = m.AsientoId,
            UsuarioId = m.UsuarioId,
            Estado = EstadoReserva.Rechazada,
            Motivo = MotivoRechazo.AsientoOcupado,
            CreadaEn = ahora
        };
        await _repo.AgregarAsync(rechazada);
        return rechazada;
    }
}