using Booking.Data.Entities;

namespace Booking.Data.Repositories;

public interface IReservaRepository
{
    Task<Reserva?> ObtenerPorSolicitudAsync(Guid solicitudId);
    Task AgregarAsync(Reserva reserva);
    // False = el asiento ya tiene una reserva activa
    Task<bool> TryAgregarRetenidaAsync(Reserva reserva);
    // Cambio de estado condicional: solo si la reserva sigue en "desde". False = alguien la cambio antes
    Task<bool> TransicionarAsync(Guid reservaId, EstadoReserva desde, EstadoReserva hacia, MotivoRechazo? motivo = null);
    // Pasa a Vencida todas las Retenidas con VenceEn < ahoraUtc. Devuelve cuantas vencio
    Task<int> VencerRetenidasAsync(DateTime ahoraUtc);
}