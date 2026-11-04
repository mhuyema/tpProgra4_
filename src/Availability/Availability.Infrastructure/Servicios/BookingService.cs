using System.Net.Http.Json;
using Availability.Application.DTOs;
using Availability.Application.Interfaces;

namespace Availability.Infrastructure.Servicios
{
    public class BookingService : IConsultaReservas
    {
        private readonly HttpClient _httpClient;
        private readonly IAuditoriaPublicador _auditoria;
        public BookingService(HttpClient httpClient, IAuditoriaPublicador auditoria)
        {
            _httpClient = httpClient;
            _auditoria = auditoria;
        }

        public async Task<EstadoSolicitudDTO> ObtenerEstadoAsync(Guid solicitudId)
        {
            try
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
            catch (Exception ex)
            {
                // El RequestId es el correlationId de la cadena Availability -> Booking.
                await _auditoria.EmitirErrorAsync(solicitudId.ToString(), "ConsultarEstadoEnBooking", ex);
                throw;
            }
        }

        // Contrato de Booking (EstadoSolicitudResponse); se traduce a EstadoSolicitudDTO.
        private record RespuestaBooking(Guid RequestId, string Status, string? RejectionReason);

        public async Task<AsientosOcupadosResponseDTO> ObtenerAsientosOcupadosAsync(int eventoId)
        {
            try
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
            catch (Exception ex)
            {
                await _auditoria.EmitirErrorAsync(Guid.NewGuid().ToString(), "ConsultarAsientosOcupadosEnBooking", ex,
                    new Dictionary<string, object?> { ["EventoId"] = eventoId });
                throw;
            }
        }
    }
}
