using Booking.Data.Entities;

namespace Booking.Data.Repositories;

public interface IReservaRepository
{
    Task<Reserva?> ObtenerPorSolicitudAsync(Guid solicitudId);
    Task AgregarAsync(Reserva reserva);
    // False = el asiento ya tiene una reserva activa
    Task<bool> TryAgregarRetenidaAsync(Reserva reserva);
}