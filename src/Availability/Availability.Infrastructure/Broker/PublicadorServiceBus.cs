using Availability.Application.Interfaces;
using Availability.Domain.Entities;
using Broker.Messaging.Abstractions;
using Broker.Messaging.Contracts;

namespace Availability.Infrastructure.Broker
{
    /// <summary>
    /// Implementación de IPublicadorSolicitudes con Azure Service Bus: traduce la solicitud
    /// al contrato compartido ReservationRequestedMessage. El RequestId viaja como correlationId.
    /// </summary>
    public class PublicadorServiceBus(IMessagePublisher publisher) : IPublicadorSolicitudes
    {
        public Task PublicarAsync(SolicitudReserva solicitud) =>
            publisher.PublishAsync(
                new ReservationRequestedMessage(
                    solicitud.RequestId,
                    solicitud.EventoId,
                    solicitud.AsientoId,
                    solicitud.UsuarioId,
                    solicitud.Monto),
                solicitud.RequestId.ToString());
    }
}