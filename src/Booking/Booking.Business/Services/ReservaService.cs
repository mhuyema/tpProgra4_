
using Booking.Business.Messages;
using Booking.Business.Notifications;
using Booking.Business.Payments;
using Booking.Data.Entities;
using Booking.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace Booking.Business.Services;

// Dominio anemico: las reglas viven aca, no en la entidad Reserva
public class ReservaService : IReservaService
{
    private static readonly TimeSpan TiempoRetencion = TimeSpan.FromMinutes(5);

    // Margen final de la retención para decidir el rechazo por
    // indisponibilidad de Payment, después de los reintentos.
    private static readonly TimeSpan MargenPagoNoDisponible =
        TimeSpan.FromSeconds(30);

    private readonly IReservaRepository _repo;
    private readonly IPagoGateway _pagoGateway;
    private readonly INotificadorReservas _notificador;
    private readonly ILogger<ReservaService> _logger;

    // Recibe el repositorio, los contratos internos de pagos y notificaciones y el servicio de logging
    // mediante inyección de dependencias.
    public ReservaService(
        IReservaRepository repo,
        IPagoGateway pagoGateway,
        INotificadorReservas notificador,
        ILogger<ReservaService> logger)
    {
        _repo = repo;
        _pagoGateway = pagoGateway;
        _notificador = notificador;
        _logger = logger;
    }

    // Se adapta el método original al contrato actualizado con Availability:
    // RequestId identifica la solicitud y Amount aporta el importe de la reserva.
    // Se conservan las reglas de idempotencia, retención, rechazo y auditoría.
    // El procesamiento del cobro se delega a ProcesarPagoAsync para mantener
    // separadas la creación de la reserva y la comunicación con Payment.
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

            // Aviso 1 del punto 3.h: la reserva quedó creada. Solo se llega acá cuando ESTE mensaje
            // la creó; una re-entrega devuelve la existente más arriba y no vuelve a notificar.
            await NotificarSinFallarAsync("ReservaCreada", reserva,
                () => _notificador.NotificarReservaCreadaAsync(reserva));

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

    public async Task<Reserva> RechazarPorTokenAsync(SolicitudReservaMessage m)
    {
        // Idempotencia: una re-entrega devuelve la misma reserva rechazada
        var existente = await _repo.ObtenerPorSolicitudAsync(m.RequestId);
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
            Motivo = MotivoRechazo.TokenInvalido,
            CreadaEn = DateTime.UtcNow
        };

        await _repo.AgregarAsync(rechazada);
        _logger.LogWarning("Reserva {ReservaId} Rechazada: identidad no verificada (token ausente, inválido o de otro usuario)",
            rechazada.Id);
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
        // Implementación original:
        // var cantidad = await _repo.VencerRetenidasAsync(DateTime.UtcNow);

        // El repositorio ahora devuelve las reservas efectivamente vencidas,
        // para auditar el cierre individual de cada solicitud.
        var vencidas = await _repo.VencerRetenidasAsync(DateTime.UtcNow);

        foreach (var reserva in vencidas)
        {
            _logger.LogInformation(
                "[AUDITORIA] FIN de procesamiento. SolicitudId={SolicitudId} ReservaId={ReservaId} EstadoFinal=Vencida",
                reserva.SolicitudId,
                reserva.ReservaId);
        }

        var cantidad = vencidas.Count;

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

    // Consulta los identificadores de los asientos ocupados de un evento.
    // Delega al repositorio el filtrado de reservas Confirmadas y
    // Retenidas vigentes, utilizando la fecha y hora actual en UTC.
    public Task<IReadOnlyList<int>> ObtenerAsientosOcupadosPorEventoAsync(
        int eventoId)
    {
        return _repo.ObtenerAsientosOcupadosPorEventoAsync(
            eventoId,
            DateTime.UtcNow);
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

        // Auditoría: identifica el inicio o la reanudación del intento
        // y permite correlacionarlo con la solicitud original.
        _logger.LogInformation(
            "[AUDITORIA] Inicio/reanudación de pago. SolicitudId={SolicitudId} ReservaId={ReservaId} IntentoPagoId={IntentoPagoId}",
            solicitudId, reserva.Id, intentoPagoId);

        // El identificador y el importe provienen de la reserva persistida.
        // Ante una falla de comunicación, el intento puede retomarse sin duplicarlo.
        ResultadoPago resultado;

        try
        {
            resultado = await _pagoGateway.ProcesarPagoAsync(
                reserva.Id,
                reserva.Importe,
                intentoPagoId);
        }
        catch (PagoComunicacionException ex)
        {
            // Una respuesta perdida no equivale a un pago rechazado.
            // Se mantiene el mismo intento para recuperarlo posteriormente.
            _logger.LogWarning(
                ex,
                "[AUDITORIA] Sin respuesta de Payment. SolicitudId={SolicitudId} ReservaId={ReservaId} IntentoPagoId={IntentoPagoId}",
                solicitudId,
                reserva.Id,
                intentoPagoId);

            var actual = await ObtenerYVerificarVencimientoAsync(solicitudId);

            if (actual.Estado != EstadoReserva.Retenida)
                return actual;

            // Si la comunicación continúa fallando cerca del vencimiento,
            // se registra PagoNoDisponible y se libera la retención.
            if (actual.VenceEn <= DateTime.UtcNow + MargenPagoNoDisponible)
            {
                var rechazada = await _repo.TryActualizarEstadoAsync(
                    actual.Id,
                    EstadoReserva.Retenida,
                    EstadoReserva.Rechazada,
                    MotivoRechazo.PagoNoDisponible);

                if (rechazada)
                {
                    // Auditoría: se identifica la solicitud cuyo ciclo
                    // terminó por indisponibilidad del servicio de pagos.
                    _logger.LogWarning(
                        "[AUDITORIA] FIN de procesamiento. SolicitudId={SolicitudId} ReservaId={ReservaId} EstadoFinal=Rechazada Motivo=PagoNoDisponible",
                        solicitudId,
                        actual.Id);
                }

                return await ObtenerYVerificarVencimientoAsync(solicitudId);
            }

            // Todavía hay tiempo para recuperar el resultado del mismo intento.
            return actual;
        }

        if (resultado.Estado == EstadoPago.EnProceso)
        {
            // El pago todavía no tiene resultado definitivo.
            // El worker retomará posteriormente el mismo intento.
            _logger.LogInformation(
                "[AUDITORIA] Payment continúa procesando. SolicitudId={SolicitudId} ReservaId={ReservaId} IntentoPagoId={IntentoPagoId}",
                solicitudId, reserva.Id, intentoPagoId);

            return await ObtenerYVerificarVencimientoAsync(solicitudId);
        }

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

        // Se registra si la transición fue efectivamente aplicada.
        // Otra réplica o el worker de vencimientos pudo haberla realizado antes.
        var transicionAplicada = await _repo.TryActualizarEstadoAsync(
            reserva.Id,
            EstadoReserva.Retenida,
            estadoNuevo,
            motivo);

        if (transicionAplicada)
        {
            _logger.LogInformation(
                "[AUDITORIA] FIN de procesamiento. SolicitudId={SolicitudId} ReservaId={ReservaId} EstadoFinal={Estado} Motivo={Motivo} IntentoPagoId={IntentoPagoId}",
                solicitudId, reserva.Id, estadoNuevo, motivo, intentoPagoId);
        }
        else
        {
            _logger.LogWarning(
                "[AUDITORIA] No se aplicó la transición del pago. SolicitudId={SolicitudId} ReservaId={ReservaId} EstadoSolicitado={Estado}",
                solicitudId, reserva.Id, estadoNuevo);
        }

        // Aviso 2 del punto 3.h: el pago fue exitoso y la reserva quedó Confirmada.
        // Solo si la transición se aplicó en este proceso (el UPDATE atómico lo garantiza),
        // así no se notifica dos veces si el worker de recuperación retoma el mismo intento.
        if (transicionAplicada && estadoNuevo == EstadoReserva.Confirmada)
        {
            await NotificarSinFallarAsync("ReservaConfirmada", reserva,
                () => _notificador.NotificarReservaConfirmadaAsync(reserva));
        }

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

    // Los avisos son "best effort": la fuente de verdad es el estado de la reserva (Availability lo
    // consulta por GET). Si el broker falla al publicar, se audita el error pero NO se deshace ni se
    // reintenta la reserva ni el cobro: un aviso perdido no debe afectar un pago ya registrado.
    // Mejora posible: patrón Outbox (marcar en la reserva qué avisos faltan y publicarlos desde un worker).
    private async Task NotificarSinFallarAsync(string tipo, Reserva reserva, Func<Task> publicar)
    {
        try
        {
            await publicar();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AUDITORIA] No se pudo publicar el aviso {Tipo}. SolicitudId={SolicitudId} ReservaId={ReservaId}",
                tipo, reserva.SolicitudId, reserva.Id);
        }
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
            // Registra el cierre cuando el vencimiento se detecta
            // durante el procesamiento o recuperación del pago.
            var vencimientoAplicado = await _repo.TryActualizarEstadoAsync(
                reserva.Id,
                EstadoReserva.Retenida,
                EstadoReserva.Vencida,
                null);

            if (vencimientoAplicado)
            {
                _logger.LogInformation(
                    "[AUDITORIA] FIN de procesamiento. SolicitudId={SolicitudId} ReservaId={ReservaId} EstadoFinal=Vencida",
                    solicitudId, reserva.Id);
            }

            reserva = await _repo.ObtenerPorSolicitudAsync(solicitudId)
                ?? throw new InvalidOperationException(
                    "No se pudo recuperar la reserva después de verificar su vencimiento.");
        }

        return reserva;
    }
}
