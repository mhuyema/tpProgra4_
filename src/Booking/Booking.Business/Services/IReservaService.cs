
using Booking.Business.Messages;
using Booking.Data.Entities;

namespace Booking.Business.Services;

public interface IReservaService
{
    // Paso 4: crea la reserva Retenida o Rechazada(AsientoOcupado). Idempotente por SolicitudId
    Task<Reserva> ProcesarSolicitudAsync(SolicitudReservaMessage mensaje);

    // Identidad no verificada (token ausente, inválido, vencido o de otro usuario): crea la reserva
    // ya Rechazada(TokenInvalido), sin retener el asiento. Idempotente por SolicitudId
    Task<Reserva> RechazarPorTokenAsync(SolicitudReservaMessage mensaje);

    // Paso 6: Retenida -> Confirmada. False = ya no estaba Retenida (ej. vencio)
    Task<bool> ConfirmarAsync(Guid reservaId);

    // Paso 6: Retenida -> Rechazada(PagoFallido | PagoNoDisponible)
    Task<bool> RechazarAsync(Guid reservaId, MotivoRechazo motivo);

    // Paso 8: Retenida vencida -> Vencida. Devuelve cuantas vencio
    Task<int> VencerReservasAsync();

    // Consulta una reserva mediante el identificador generado por Availability.
    // Paso 7: seguimiento del resultado de una solicitud
    Task<Reserva?> ObtenerPorSolicitudAsync(Guid solicitudId);

    // Consulta los identificadores de los asientos ocupados de un evento.
    // Incluye Confirmadas y Retenidas que todavía no hayan vencido.
    Task<IReadOnlyList<int>> ObtenerAsientosOcupadosPorEventoAsync(
        int eventoId);

    // Procesa o retoma el pago de una reserva mediante su identificador de solicitud.
    Task<Reserva> ProcesarPagoAsync(Guid solicitudId);
}
