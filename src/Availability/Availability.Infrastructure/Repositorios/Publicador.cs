using Availability.Application.Interfaces;
using Availability.Domain.Entities;
using Broker.Messaging.Abstractions;
using Broker.Messaging.Contracts;


namespace Availability.Infrastructure.Repositorios
{
    /// <summary>
    /// Implementación de IPublicadorSolicitudes: publica la solicitud en el broker (Azure Service Bus).
    /// </summary>
    public class Publicador(IMessagePublisher broker) : IPublicadorSolicitudes
    {
        public Task PublicarAsync(SolicitudReserva solicitud)
        {
            var mensaje = new ReservationRequestedMessage(
                solicitud.RequestId, solicitud.EventoId, solicitud.AsientoId, solicitud.UsuarioId);

            return broker.PublishAsync(mensaje, solicitud.RequestId.ToString());
        }
    }
}
