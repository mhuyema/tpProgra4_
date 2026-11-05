using System.Net.Http.Json;
using Availability.Application.DTOs;
using Availability.Application.Interfaces;

namespace Availability.Infrastructure.Servicios
{
    /// <summary>
    /// Implementación de IConsultaNotificaciones: llama por HTTP al microservicio de Notificaciones.
    ///   GET {Notifications:BaseUrl}{usuarioId}   con el Bearer del usuario logueado.
    ///
    /// Este cliente HTTP lo configura DependencyInjection (la URL base) y Availability.Web le agrega el token
    /// del usuario con TokenUsuarioHandler. Gracias a eso esta clase no se ocupa de la autenticación.
    /// </summary>
    public class NotificacionesService : IConsultaNotificaciones
    {
        private readonly HttpClient _httpClient;

        // HttpClient llega por inyección de dependencias: .NET lo crea ya configurado (BaseAddress, handlers).
        public NotificacionesService(HttpClient httpClient) => _httpClient = httpClient;

        public async Task<IReadOnlyList<NotificacionDTO>> ObtenerPorUsuarioAsync(string usuarioId)
        {
            // Uri.EscapeDataString protege el id por si tuviera caracteres raros para una URL.
            var response = await _httpClient.GetAsync(Uri.EscapeDataString(usuarioId));

            // Si Notifications responde un error (401, 403, 503...) se lanza una excepción; el caso de uso
            // ConsultarNotificaciones la atrapa para que la pantalla de seguimiento siga funcionando.
            response.EnsureSuccessStatusCode();

            var avisos = await response.Content.ReadFromJsonAsync<List<RespuestaNotificacion>>()
                         ?? new List<RespuestaNotificacion>();

            // Se traduce el contrato de Notifications (inglés) al DTO propio de Availability (español).
            // Así, si Notifications cambia sus nombres, el único archivo que se toca es este.
            return avisos
                .Select(a => new NotificacionDTO(a.RequestId, a.Type, a.Message, a.SentAtUtc))
                .ToList();
        }

        // Forma del JSON que devuelve Notifications (record Notification). Solo sirve para leer la respuesta.
        private record RespuestaNotificacion(Guid RequestId, string Type, string UserId, string Message, DateTime SentAtUtc);
    }
}
