using Booking.Business.Auditing;
using Booking.Business.Auth;
using Booking.Business.Messages;
using Booking.Business.Services;
using Booking.Data.Entities;
using Broker.Messaging.Abstractions;
using Broker.Messaging.Contracts;

namespace Booking.Api.Handlers;

// Adaptador de entrada: recibe el mensaje COMPARTIDO del broker (contrato en ingles, de Broker.Messaging)
// y lo traduce al comando propio de Booking (SolicitudReservaMessage). Asi la capa Business
// no depende del broker ni de su contrato: si el contrato cambia, solo se toca este archivo.
//
// Si HandleAsync lanza una excepcion, el suscriptor abandona el mensaje y el broker lo reentrega
// (at-least-once). Es seguro porque ProcesarSolicitudAsync es idempotente por SolicitudId.
public class ReservationRequestedHandler : IMessageHandler<ReservationRequestedMessage>
{
    private readonly IReservaService _service;
    private readonly IValidadorToken _validadorToken;
    private readonly ILogger<ReservationRequestedHandler> _logger;
    private readonly IBookingAuditPublisher _audit;

    public ReservationRequestedHandler(IReservaService service, IValidadorToken validadorToken, ILogger<ReservationRequestedHandler> logger, IBookingAuditPublisher audit)
    {
        _audit = audit;
        _service = service;
        _validadorToken = validadorToken;
        _logger = logger;
    }

    public async Task HandleAsync(ReservationRequestedMessage message, string correlationId, CancellationToken cancellationToken)
    {
        // Auditoria: el correlationId (= RequestId) acompana a TODOS los logs de esta cadena de sucesos,
        // asi se puede seguir una solicitud de punta a punta filtrando por ese id.
        using var logScope = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId });

        // Mensaje invalido: reintentarlo no lo arregla (poison message). Se registra y se descarta
        // (retornar confirma el mensaje) para que no cicle hasta agotar las entregas.
        if (message.RequestId == Guid.Empty || string.IsNullOrWhiteSpace(message.UserId))
        {
            _logger.LogError("[AUDITORIA] Mensaje incompleto, se descarta. RequestId={RequestId} UserId={UserId}",
                message.RequestId, message.UserId);
            await _audit.EmitErrorAsync(correlationId, "ProcesarSolicitudReserva",
                new InvalidOperationException("Mensaje incompleto, se descarta."),
                new Dictionary<string, object?> { ["RequestId"] = message.RequestId, ["UserId"] = message.UserId },
                cancellationToken);
            return;
        }

        // Registra explícitamente el inicio de la solicitud y su identificador,
        // independientemente de que el proveedor de logs muestre el scope.
        _logger.LogInformation(
            "[AUDITORIA] INICIO de procesamiento. SolicitudId={SolicitudId} Evento={EventoId} Asiento={AsientoId} Usuario={UsuarioId}",
            message.RequestId, message.EventId, message.SeatId, message.UserId);

        await _audit.InitProcessAsync(correlationId, "ProcesarSolicitudReserva",
            new Dictionary<string, object?> { ["EventoId"] = message.EventId, ["AsientoId"] = message.SeatId, ["UsuarioId"] = message.UserId },
            cancellationToken);

        try
        {
        var solicitud = new SolicitudReservaMessage(
                     message.RequestId,
                     message.EventId,
                     message.SeatId,
                     message.UserId,
                     message.Amount);

        // Verificación de identidad (validación delegada en el Authorization Server). El token debe ser válido
        // Y pertenecer al usuario que dice el mensaje (evita suplantar a otro UserId).
        // Si no lo es, reintentar no lo arregla: se rechaza la solicitud y se confirma el mensaje.
        // Si el Authorization Server no responde, ValidarAsync lanza y el broker reentrega.
        // Nunca se loguea el token.
        var token = await _validadorToken.ValidarAsync(message.AccessToken, cancellationToken);
        if (!token.Valido || !string.Equals(token.Subject, message.UserId, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "[AUDITORIA] Identidad no verificada, se rechaza. SolicitudId={SolicitudId} TokenPresente={TokenPresente} TokenValido={TokenValido}",
                message.RequestId, !string.IsNullOrWhiteSpace(message.AccessToken), token.Valido);
            await _service.RechazarPorTokenAsync(solicitud);
            return;
        }

        // Línea original: procesaba la solicitud, pero no iniciaba el cobro.
        // var reserva = await _service.ProcesarSolicitudAsync(solicitud);

        // TODO (cobro): si reserva.Estado == Retenida, aca se llama por gRPC a Payment y segun la respuesta
        // se invoca _service.ConfirmarAsync / _service.RechazarAsync (APPROVED / REJECTED / Payment caido).
        // Ojo: si el mensaje es una re-entrega, la reserva puede estar ya Confirmada/Rechazada:
        // solo cobrar si sigue Retenida.

        // Cambio de lógica: se conserva el procesamiento inicial y se delega el cobro
        // a ProcesarPagoAsync, que ya gestiona las respuestas de Payment y reutiliza
        // IntentoPagoId ante reentregas. Solo se invoca si la reserva sigue Retenida.

        var reserva = await _service.ProcesarSolicitudAsync(solicitud);

        if (reserva.Estado == EstadoReserva.Retenida)
        {
            reserva = await _service.ProcesarPagoAsync(reserva.SolicitudId);
        }

        // Identifica el cierre del procesamiento del mensaje.
        // Si la reserva sigue Retenida, su ciclo todavía no ha finalizado:
        // el seguimiento continúa mediante PagosPendientesWorker.
        _logger.LogInformation(
            "[AUDITORIA] FIN del procesamiento del mensaje. SolicitudId={SolicitudId} ReservaId={ReservaId} EstadoActual={Estado} Motivo={Motivo}",
            reserva.SolicitudId, reserva.Id, reserva.Estado, reserva.Motivo);

        await _audit.EndProcessAsync(correlationId, "ProcesarSolicitudReserva",
            new { Estado = reserva.Estado.ToString(), Motivo = reserva.Motivo?.ToString() },
            new Dictionary<string, object?> { ["ReservaId"] = reserva.Id },
            cancellationToken);
        }
        catch (Exception ex)
        {
            // Se audita y se relanza: el suscriptor abandona el mensaje y el broker lo reentrega.
            await _audit.EmitErrorAsync(correlationId, "ProcesarSolicitudReserva", ex, null, cancellationToken);
            throw;
        }
    }
}