using Booking.Business.Messages;
using Booking.Business.Payments;
using Booking.Data.Entities;
using Booking.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace Booking.Business.Services;

// Dominio anemico: las reglas viven aca, no en la entidad Reserva
public class ReservaService : IReservaService
{
    private static readonly TimeSpan TiempoRetencion = TimeSpan.FromMinutes(5);
    private readonly IReservaRepository _repo;
    private readonly IPagoGateway _pagoGateway;
    private readonly ILogger<ReservaService> _logger;

    // Recibe el repositorio, el contrato interno de pagos y el servicio de logging
    // mediante inyección de dependencias.
    public ReservaService(
        IReservaRepository repo,
        IPagoGateway pagoGateway,
        ILogger<ReservaService> logger)
    {
        _repo = repo;
        _pagoGateway = pagoGateway;
        _logger = logger;
    }

    public async Task<Reserva> ProcesarSolicitudAsync(SolicitudReservaMessage m)
    {
        // Idempotencia: si el broker entrega el mensaje dos veces, devolvemos la misma reserva
        var existente = await _repo.ObtenerPorSolicitudAsync(m.RequestId);
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
            SolicitudId = m.RequestId,
            EventoId = m.EventId,
            AsientoId = m.SeatId,
            UsuarioId = m.UserId,
            Importe = m.Amount,
            Estado = EstadoReserva.Retenida,
            CreadaEn = ahora,
            VenceEn = ahora + TiempoRetencion
        };

        // La base decide quien gana si dos replicas compiten por el mismo asiento
        if (await _repo.TryAgregarRetenidaAsync(reserva))
        {
            _logger.LogInformation("Reserva {ReservaId} Retenida hasta {VenceEn:O}",
                reserva.Id, reserva.VenceEn);
            return reserva;
        }

        // Fallo: puede ser asiento ocupado, o el mismo mensaje procesado en paralelo
        existente = await _repo.ObtenerPorSolicitudAsync(m.RequestId);
        if (existente is not null) return existente;

        var rechazada = new Reserva
        {
            Id = Guid.NewGuid(),
            SolicitudId = m.RequestId,
            EventoId = m.EventId,
            AsientoId = m.SeatId,
            UsuarioId = m.UserId,
            Importe = m.Amount,
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
        var ok = await _repo.TransicionarAsync(
            reservaId,
            EstadoReserva.Retenida,
            EstadoReserva.Confirmada);

        if (ok)
            _logger.LogInformation("Reserva {ReservaId} Confirmada", reservaId);
        else
            _logger.LogWarning(
                "No se pudo confirmar {ReservaId}: ya no estaba Retenida (vencida o resuelta)",
                reservaId);

        return ok;
    }

    public async Task<bool> RechazarAsync(Guid reservaId, MotivoRechazo motivo)
    {
        var ok = await _repo.TransicionarAsync(
            reservaId,
            EstadoReserva.Retenida,
            EstadoReserva.Rechazada,
            motivo);

        if (ok)
            _logger.LogInformation("Reserva {ReservaId} Rechazada. Motivo={Motivo}",
                reservaId, motivo);
        else
            _logger.LogWarning(
                "No se pudo rechazar {ReservaId}: ya no estaba Retenida",
                reservaId);

        return ok;
    }

    public async Task<int> VencerReservasAsync()
    {
        var cantidad = await _repo.VencerRetenidasAsync(DateTime.UtcNow);

        if (cantidad > 0)
            _logger.LogInformation(
                "[AUDITORIA] {Cantidad} reservas pasaron a Vencida y liberaron su asiento",
                cantidad);

        return cantidad;
    }

    // Consulta el estado de una solicitud sin acceder directamente a la base de datos.
    public Task<Reserva?> ObtenerPorSolicitudAsync(Guid solicitudId)
    {
        return _repo.ObtenerPorSolicitudAsync(solicitudId);
    }

    // Inicia o retoma el pago de una reserva retenida, reutilizando
    // el identificador del intento previamente almacenado.
    public async Task<Reserva> ProcesarPagoAsync(Guid solicitudId)
    {
        var reserva = await ObtenerYVerificarVencimientoAsync(solicitudId);

        // Solo las reservas retenidas pueden iniciar o continuar un pago.
        if (reserva.Estado != EstadoReserva.Retenida)
            return reserva;

        if (reserva.IntentoPagoId is null)
        {
            // La asignación es atómica: ante dos procesos simultáneos,
            // ambos terminarán utilizando el identificador persistido.
            await _repo.TryAsignarIntentoPagoAsync(
                reserva.Id,
                Guid.NewGuid());

            reserva = await ObtenerYVerificarVencimientoAsync(solicitudId);

            if (reserva.Estado != EstadoReserva.Retenida)
                return reserva;
        }

        var intentoPagoId = reserva.IntentoPagoId
            ?? throw new InvalidOperationException(
                "No se pudo recuperar el identificador del intento de pago.");

        // El identificador y el importe provienen de la reserva persistida.
        // Ante una falla de comunicación, el intento puede retomarse sin duplicarlo.
        var resultado = await _pagoGateway.ProcesarPagoAsync(
            reserva.Id,
            reserva.Importe,
            intentoPagoId);

        if (resultado.Estado == EstadoPago.EnProceso)
            return await ObtenerYVerificarVencimientoAsync(solicitudId);

        // Traduce el resultado del pago a la transición de estado correspondiente.
        var estadoNuevo = resultado.Estado switch
        {
            EstadoPago.Aprobado => EstadoReserva.Confirmada,
            EstadoPago.Rechazado => EstadoReserva.Rechazada,
            _ => throw new InvalidOperationException(
                $"Estado de pago no reconocido: {resultado.Estado}")
        };

        var motivo = resultado.Estado == EstadoPago.Rechazado
            ? MotivoRechazo.PagoFallido
            : (MotivoRechazo?)null;

        await _repo.TryActualizarEstadoAsync(
            reserva.Id,
            EstadoReserva.Retenida,
            estadoNuevo,
            motivo);

        // Se consulta el estado real porque otro proceso pudo modificarlo.
        reserva = await ObtenerYVerificarVencimientoAsync(solicitudId);

        // Una aprobación que no pudo confirmarse requiere tratamiento posterior:
        // no debemos ocultar un posible cobro de una reserva ya vencida.
        if (resultado.Estado == EstadoPago.Aprobado &&
            reserva.Estado != EstadoReserva.Confirmada)
        {
            throw new InvalidOperationException(
                "Payment aprobó el pago, pero Booking no pudo confirmar la reserva.");
        }

        if (resultado.Estado == EstadoPago.Rechazado &&
            reserva.Estado != EstadoReserva.Rechazada &&
            reserva.Estado != EstadoReserva.Vencida)
        {
            throw new InvalidOperationException(
                "Booking no pudo registrar el rechazo del pago.");
        }

        return reserva;
    }

    // Comprueba el vencimiento antes de continuar con el pago y
    // recupera el estado persistido si otro proceso modificó la reserva.
    private async Task<Reserva> ObtenerYVerificarVencimientoAsync(Guid solicitudId)
    {
        var reserva = await _repo.ObtenerPorSolicitudAsync(solicitudId)
            ?? throw new InvalidOperationException(
                $"No existe una reserva para la solicitud {solicitudId}.");

        if (reserva.Estado != EstadoReserva.Retenida)
            return reserva;

        if (reserva.VenceEn is null)
            throw new InvalidOperationException(
                "La reserva retenida no tiene fecha de vencimiento.");

        if (reserva.VenceEn <= DateTime.UtcNow)
        {
            await _repo.TryActualizarEstadoAsync(
                reserva.Id,
                EstadoReserva.Retenida,
                EstadoReserva.Vencida,
                null);

            reserva = await _repo.ObtenerPorSolicitudAsync(solicitudId)
                ?? throw new InvalidOperationException(
                    "No se pudo recuperar la reserva después de verificar su vencimiento.");
        }

        return reserva;
    }
}