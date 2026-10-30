using Booking.Data.Entities;

namespace Booking.Data.Repositories;

public interface IReservaRepository
{
    Task<Reserva?> ObtenerPorSolicitudAsync(Guid solicitudId);

    Task AgregarAsync(Reserva reserva);

    Task<bool> TryAgregarRetenidaAsync(Reserva reserva);

    Task<bool> TryActualizarEstadoAsync(
        Guid reservaId,
        EstadoReserva estadoEsperado,
        EstadoReserva estadoNuevo,
        MotivoRechazo? motivo);

    Task<bool> TryAsignarIntentoPagoAsync(
        Guid reservaId,
        Guid intentoPagoId);

    Task<bool> TransicionarAsync(
        Guid reservaId,
        EstadoReserva desde,
        EstadoReserva hacia,
        MotivoRechazo? motivo = null);

    // Devuelve únicamente las reservas cuyo vencimiento se aplicó
    // correctamente, incluyendo sus identificadores para auditoría.
    Task<IReadOnlyList<(Guid ReservaId, Guid SolicitudId)>>
        VencerRetenidasAsync(DateTime ahoraUtc);

    Task<IReadOnlyList<Guid>> ObtenerSolicitudesConPagoPendienteAsync(
        DateTime ahoraUtc);
}