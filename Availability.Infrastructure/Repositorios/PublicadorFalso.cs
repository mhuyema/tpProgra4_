using Availability.Application.Interfaces;
using Availability.Domain.Entities;
using Microsoft.Extensions.Logging;


namespace Availability.Infrastructure.Repositorios
{
    /// <summary>
    /// Implementación provisoria de IPublicadorSolicitudes mientras no está RabbitMQ:
    /// solo deja registro en el log. Se reemplaza por la de MassTransit sin tocar Application.
    /// </summary>
    public class PublicadorFalso(ILogger<PublicadorFalso> logger) : IPublicadorSolicitudes
    {
        public Task PublicarAsync(SolicitudReserva solicitud)
        {
            logger.LogInformation("[SIMULADO] Solicitud {SolicitudId} publicada para el asiento {AsientoId}",
                solicitud.Id, solicitud.AsientoId);
            return Task.CompletedTask;
        }
    }
}
