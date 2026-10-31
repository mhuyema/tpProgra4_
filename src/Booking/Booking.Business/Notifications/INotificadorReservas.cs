using Booking.Data.Entities;

namespace Booking.Business.Notifications;

// Contrato interno para avisar al servicio de Notificaciones (punto 3.h) sin que Business
// dependa del broker ni de sus mensajes: misma idea que IPagoGateway con Payment.
// La implementación (publicar en el topic) vive en Booking.Api.
public interface INotificadorReservas
{
    // La reserva quedó creada (Retenida) y el asiento está apartado hasta VenceEn.
    Task NotificarReservaCreadaAsync(Reserva reserva);

    // El pago fue exitoso y la reserva quedó Confirmada.
    Task NotificarReservaConfirmadaAsync(Reserva reserva);
}
