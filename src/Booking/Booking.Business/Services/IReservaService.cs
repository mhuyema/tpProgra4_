using Booking.Business.Messages;
using Booking.Data.Entities;

namespace Booking.Business.Services;

public interface IReservaService
{
    // Paso 4: crea la reserva Retenida o Rechazada(AsientoOcupado). Idempotente por SolicitudId
    Task<Reserva> ProcesarSolicitudAsync(SolicitudReservaMessage mensaje);
    // Paso 6: Retenida -> Confirmada. False = ya no estaba Retenida (ej. vencio)
    Task<bool> ConfirmarAsync(Guid reservaId);
    // Paso 6: Retenida -> Rechazada(PagoFallido | PagoNoDisponible)
    Task<bool> RechazarAsync(Guid reservaId, MotivoRechazo motivo);
    // Paso 8: Retenida vencida -> Vencida. Devuelve cuantas vencio
    Task<int> VencerReservasAsync();
    // Paso 7: seguimiento del resultado de una solicitud
    Task<Reserva?> ObtenerPorSolicitudAsync(Guid solicitudId);
}