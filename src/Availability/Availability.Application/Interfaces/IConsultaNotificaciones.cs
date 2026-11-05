using Availability.Application.DTOs;

namespace Availability.Application.Interfaces
{
    /// <summary>
    /// Puerto de salida hacia el servicio de Notificaciones (dueño de los avisos al usuario).
    ///
    /// Clean Architecture: la capa Application (donde estamos) define QUÉ necesita ("dame los avisos
    /// de este usuario") pero no sabe CÓMO se consigue. El cómo (una llamada HTTP) lo hace la capa
    /// Infrastructure. Es la misma idea que ya usa IConsultaReservas con Booking.
    /// </summary>
    public interface IConsultaNotificaciones
    {
        Task<IReadOnlyList<NotificacionDTO>> ObtenerPorUsuarioAsync(string usuarioId);
    }
}
