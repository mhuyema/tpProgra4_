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

            response.EnsureSuccessStatusCode();

            var estado = await response.Content
                .ReadFromJsonAsync<EstadoSolicitudDTO>();

            return estado ?? throw new Exception(
                "No se pudo obtener el estado de la solicitud."
            );
        }

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
