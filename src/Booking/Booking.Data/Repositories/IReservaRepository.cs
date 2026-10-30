using Booking.Data.Entities;

namespace Booking.Data.Repositories;

public interface IReservaRepository
{
    Task<Reserva?> ObtenerPorSolicitudAsync(Guid solicitudId);
    Task AgregarAsync(Reserva reserva);

    // False = el asiento ya tiene una reserva activa
    Task<bool> TryAgregarRetenidaAsync(Reserva reserva);

    // Actualiza el estado de una reserva solo si conserva el estado esperado.
    // Evita sobrescribir cambios realizados por otros procesos.
    Task<bool> TryActualizarEstadoAsync(
        Guid reservaId,
        EstadoReserva estadoEsperado,
        EstadoReserva estadoNuevo,
        MotivoRechazo? motivo);

    // Asigna un identificador de intento de pago únicamente si la reserva
    // sigue retenida, no venció y todavía no tiene un intento registrado.
    Task<bool> TryAsignarIntentoPagoAsync(Guid reservaId, Guid intentoPagoId);

    // Cambio de estado condicional: solo si la reserva sigue en "desde". False = alguien la cambio antes
    Task<bool> TransicionarAsync(
        Guid reservaId,
        EstadoReserva desde,
        EstadoReserva hacia,
        MotivoRechazo? motivo = null);

    // Pasa a Vencida todas las Retenidas con VenceEn < ahoraUtc. Devuelve cuantas vencio
    Task<int> VencerRetenidasAsync(DateTime ahoraUtc);
}