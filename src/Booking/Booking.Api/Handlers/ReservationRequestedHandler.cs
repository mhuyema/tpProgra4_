using Booking.Business.Messages;
using Booking.Business.Services;
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
    private readonly ILogger<ReservationRequestedHandler> _logger;

    public ReservationRequestedHandler(IReservaService service, ILogger<ReservationRequestedHandler> logger)
    {
        _service = service;
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
            return;
        }

        _logger.LogInformation("[AUDITORIA] Booking procesa la solicitud. Evento={EventoId} Asiento={AsientoId} Usuario={UsuarioId}",
            message.EventId, message.SeatId, message.UserId);

        var solicitud = new SolicitudReservaMessage(
                     message.RequestId,
                     message.EventId,
                     message.SeatId,
                     message.UserId,
                     message.Amount);

        var reserva = await _service.ProcesarSolicitudAsync(solicitud);

        // TODO (cobro): si reserva.Estado == Retenida, aca se llama por gRPC a Payment y segun la respuesta
        // se invoca _service.ConfirmarAsync / _service.RechazarAsync (APPROVED / REJECTED / Payment caido).
        // Ojo: si el mensaje es una re-entrega, la reserva puede estar ya Confirmada/Rechazada:
        // solo cobrar si sigue Retenida.

        _logger.LogInformation("[AUDITORIA] Solicitud procesada. Reserva={ReservaId} Estado={Estado} Motivo={Motivo}",
            reserva.Id, reserva.Estado, reserva.Motivo);
    }
}
