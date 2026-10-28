using Availability.Domain.Entities;


namespace Availability.Application.Interfaces
{
    /// <summary>
    /// Registro de las solicitudes de reserva.
    /// </summary>
    public interface ISolicitudReservaRepositorio
    {
        Task GuardarAsync(SolicitudReserva solicitud);
        Task<SolicitudReserva?> ObtenerPorIdAsync(Guid id);
    }
}
