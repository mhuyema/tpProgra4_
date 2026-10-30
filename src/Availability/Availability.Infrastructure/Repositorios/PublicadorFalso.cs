using Availability.Application.Interfaces;
using Availability.Domain.Entities;
using Microsoft.Extensions.Logging;


namespace Availability.Infrastructure.Repositorios
{
    /// <summary>
    /// Implementación provisoria de IPublicadorSolicitudes: solo deja registro en el log.
    /// </summary>
    public class PublicadorFalso(ILogger<PublicadorFalso> logger) : IPublicadorSolicitudes
    {
        public Task PublicarAsync(SolicitudReserva solicitud)
        {
            logger.LogInformation("[SIMULADO] Solicitud {SolicitudId} publicada para el asiento {AsientoId}",
                solicitud.RequestId, solicitud.AsientoId);
            return Task.CompletedTask;
        }
    }
}
