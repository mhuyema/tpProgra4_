using Booking.Business.Messages;
using Booking.Data.Entities;
using Booking.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace Booking.Business.Services;

// Dominio anemico: las reglas viven aca, no en la entidad Reserva
public class ReservaService : IReservaService
{
    private static readonly TimeSpan TiempoRetencion = TimeSpan.FromMinutes(5);
    private readonly IReservaRepository _repo;
    private readonly ILogger<ReservaService> _logger;

    public ReservaService(IReservaRepository repo, ILogger<ReservaService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<Reserva> ProcesarSolicitudAsync(SolicitudReservaMessage m)
    {
        // Idempotencia: si el broker entrega el mensaje dos veces, devolvemos la misma reserva
        var existente = await _repo.ObtenerPorSolicitudAsync(m.SolicitudId);
        if (existente is not null)
        {
            _logger.LogWarning("Mensaje duplicado, ya existe la reserva {ReservaId} en estado {Estado}",
                existente.Id, existente.Estado);
            return existente;
        }

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
        if (await _repo.TryAgregarRetenidaAsync(reserva))
        {
            _logger.LogInformation("Reserva {ReservaId} Retenida hasta {VenceEn:O}", reserva.Id, reserva.VenceEn);
            return reserva;
        }

        // Fallo: puede ser asiento ocupado, o el mismo mensaje procesado en paralelo
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
        _logger.LogInformation("Reserva {ReservaId} Rechazada: asiento {AsientoId} del evento {EventoId} ocupado",
            rechazada.Id, rechazada.AsientoId, rechazada.EventoId);
        return rechazada;
    }

    public async Task<bool> ConfirmarAsync(Guid reservaId)
    {
        var ok = await _repo.TransicionarAsync(reservaId, EstadoReserva.Retenida, EstadoReserva.Confirmada);
        if (ok) _logger.LogInformation("Reserva {ReservaId} Confirmada", reservaId);
        else _logger.LogWarning("No se pudo confirmar {ReservaId}: ya no estaba Retenida (vencida o resuelta)", reservaId);
        return ok;
    }

    public async Task<bool> RechazarAsync(Guid reservaId, MotivoRechazo motivo)
    {
        var ok = await _repo.TransicionarAsync(reservaId, EstadoReserva.Retenida, EstadoReserva.Rechazada, motivo);
        if (ok) _logger.LogInformation("Reserva {ReservaId} Rechazada. Motivo={Motivo}", reservaId, motivo);
        else _logger.LogWarning("No se pudo rechazar {ReservaId}: ya no estaba Retenida", reservaId);
        return ok;
    }

    public async Task<int> VencerReservasAsync()
    {
        var cantidad = await _repo.VencerRetenidasAsync(DateTime.UtcNow);
        if (cantidad > 0)
            _logger.LogInformation("[AUDITORIA] {Cantidad} reservas pasaron a Vencida y liberaron su asiento", cantidad);
        return cantidad;
    }

    public Task<Reserva?> ObtenerPorSolicitudAsync(Guid solicitudId) =>
        _repo.ObtenerPorSolicitudAsync(solicitudId);
}