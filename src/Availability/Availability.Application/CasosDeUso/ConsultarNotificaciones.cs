using Availability.Application.DTOs;
using Availability.Application.Interfaces;

namespace Availability.Application.CasosDeUso
{
    /// <summary>
    /// Caso de uso: ver los avisos que recibió el usuario por sus reservas (punto 3.h del TP).
    ///
    /// Un "caso de uso" es una acción concreta que el sistema ofrece. El controller no habla directo
    /// con Notificaciones: le pide este caso de uso, que sabe qué hacer si algo sale mal.
    /// </summary>
    public class ConsultarNotificaciones(
        IConsultaNotificaciones notificaciones,
        IAuditoriaPublicador auditoria)
    {
        public async Task<IReadOnlyList<NotificacionDTO>> EjecutarAsync(
            string usuarioId, IReadOnlyCollection<Guid> solicitudIds)
        {
            try
            {
                var todas = await notificaciones.ObtenerPorUsuarioAsync(usuarioId);

                // Notifications devuelve los avisos de todas las reservas del usuario; en esta pantalla
                // solo queremos los de las solicitudes que se están mostrando.
                return todas
                    .Where(n => solicitudIds.Contains(n.SolicitudId))
                    .OrderBy(n => n.EnviadaUtc)
                    .ToList();
            }
            catch (Exception ex)
            {
                // Los avisos son un extra: si Notifications está caído, la reserva igual existe y su estado
                // se ve bien. Por eso no relanzamos el error: lo registramos en el AuditLog y devolvemos una
                // lista vacía. Es la tolerancia a fallos que pide el TP (un componente caído no tira a los demás).
                var correlationId = solicitudIds.FirstOrDefault().ToString();
                await auditoria.EmitirErrorAsync(correlationId, "ConsultarNotificaciones", ex,
                    new Dictionary<string, object?> { ["UsuarioId"] = usuarioId });
                return Array.Empty<NotificacionDTO>();
            }
        }
    }
}
