using Booking.Business.Messages;
using Booking.Data.Entities;

namespace Booking.Business.Services;

public interface IReservaService
{
    Task<Reserva> ProcesarSolicitudAsync(SolicitudReservaMessage menssaje);

    // Consulta una reserva mediante el identificador generado por Availability.
    Task<Reserva?> ObtenerPorSolicitudAsync(Guid solicitudId);
}