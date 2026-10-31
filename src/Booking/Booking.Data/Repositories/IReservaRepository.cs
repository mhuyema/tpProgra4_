
using Booking.Data.Entities;

namespace Booking.Data.Repositories;

public interface IReservaRepository
{
    Task<Reserva?> ObtenerPorSolicitudAsync(Guid solicitudId);

    // Consulta los identificadores de los asientos ocupados de un evento.
    // Considera las reservas Confirmadas y las Retenidas que siguen vigentes.
    // El filtrado se realiza directamente en SQL Server.
    Task<IReadOnlyList<int>> ObtenerAsientosOcupadosPorEventoAsync(
        int eventoId,
        DateTime ahoraUtc);

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
