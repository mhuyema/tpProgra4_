using Booking.Business.Notifications;
using Booking.Data.Entities;
using Broker.Messaging.Abstractions;
using Broker.Messaging.Contracts;

namespace Booking.API.Integrations;

// Adaptador de salida: traduce la reserva de Booking a los mensajes COMPARTIDOS del broker y los
// publica en el topic (Publish/Subscribe). Booking no conoce a Notificaciones: solo escribe en el
// topic y el servicio de Notificaciones los consume desde su propia suscripción.
// El correlationId es el SolicitudId (= RequestId), igual que en el resto de la cadena de sucesos.
public class NotificadorBroker : INotificadorReservas
{
    private readonly IMessagePublisher _publisher;

    public NotificadorBroker(IMessagePublisher publisher) => _publisher = publisher;

    public Task NotificarReservaCreadaAsync(Reserva reserva) =>
        _publisher.PublishAsync(
            new ReservationCreatedMessage(
                reserva.SolicitudId,
                reserva.Id,
                reserva.EventoId,
                reserva.AsientoId,
                reserva.UsuarioId,
                reserva.Importe,
                reserva.VenceEn),
            reserva.SolicitudId.ToString());

    public Task NotificarReservaConfirmadaAsync(Reserva reserva) =>
        _publisher.PublishAsync(
            new ReservationConfirmedMessage(
                reserva.SolicitudId,
                reserva.Id,
                reserva.EventoId,
                reserva.AsientoId,
                reserva.UsuarioId,
                reserva.Importe),
            reserva.SolicitudId.ToString());
}
