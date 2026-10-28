using Availability.Application.DTOs;
using Availability.Application.Interfaces;
using  Availability.Domain.Entities;

namespace Availability.Infrastructure.Repositorios
{
    /// <summary>
    /// Implementación provisoria de IConsultaReservas mientras no está Booking:
    /// ningún asiento ocupado y toda solicitud pendiente. Se reemplaza por el cliente HTTP real.
    /// </summary>
    public class ConsultaReservasFalsa : IConsultaReservas
    {
        public Task<List<Guid>> ObtenerAsientosOcupadosAsync(Guid eventoId) =>
            Task.FromResult(new List<Guid>());

        public Task<EstadoSolicitudDTO> ObtenerEstadoAsync(Guid solicitudId) =>
            Task.FromResult(new EstadoSolicitudDTO
            (
                solicitudId,
                "Pendiente",
                null
            ));
    }
}
