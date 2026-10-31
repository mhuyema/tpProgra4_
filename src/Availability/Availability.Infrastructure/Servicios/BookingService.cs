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

        public async Task<AsientosOcupadosResponseDTO> ObtenerAsientosOcupadosAsync(int eventoId)
        {
            var response = await _httpClient.GetAsync(
                $"eventos/{eventoId}/asientos-ocupados"
            );

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return new AsientosOcupadosResponseDTO();

            response.EnsureSuccessStatusCode();

            var asientosOcupados = await response.Content
                .ReadFromJsonAsync<AsientosOcupadosResponseDTO>();

            return asientosOcupados ?? new AsientosOcupadosResponseDTO();
        }
    }
}
