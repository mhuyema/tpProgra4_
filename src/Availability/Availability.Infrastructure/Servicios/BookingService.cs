using System.Net.Http.Json;
using Availability.Application.DTOs;
using Availability.Application.Interfaces;

namespace Availability.Infrastructure.Servicios
{
    public class BookingService : IConsultaReservas
    {
        private readonly HttpClient _httpClient;
        public BookingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<EstadoSolicitudDTO> ObtenerEstadoAsync(Guid solicitudId)
        {
            var response = await _httpClient.GetAsync(
                $"solicitudes/{solicitudId}"
            );

            // Booking procesa la solicitud de forma asíncrona (broker): un 404 significa "todavía no la procesó".
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return new EstadoSolicitudDTO(solicitudId, "Pendiente", null);

            response.EnsureSuccessStatusCode();

            var estado = await response.Content
                .ReadFromJsonAsync<RespuestaBooking>();

            return estado is null
                ? throw new Exception("No se pudo obtener el estado de la solicitud.")
                : new EstadoSolicitudDTO(estado.RequestId, estado.Status, estado.RejectionReason);
        }

        // Contrato de Booking (EstadoSolicitudResponse); se traduce a EstadoSolicitudDTO.
        private record RespuestaBooking(Guid RequestId, string Status, string? RejectionReason);

        public async Task<List<int>> ObtenerAsientosOcupadosAsync(int eventoId)
        {
            // TEMPORAL: Booking todavía no expone eventos/{id}/asientos-ocupados; se devuelve vacío hasta que exista.
            //var response = await _httpClient.GetAsync(
            //    $"eventos/{eventoId}/asientos-ocupados"
            //);
            //response.EnsureSuccessStatusCode();
            //var asientosOcupados = await response.Content
            //    .ReadFromJsonAsync<List<int>>();
            //return asientosOcupados ?? new List<int>();
            return await Task.FromResult(new List<int>());
        }
    }
}
