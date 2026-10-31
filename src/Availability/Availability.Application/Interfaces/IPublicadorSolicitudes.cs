using Availability.Domain.Entities;

namespace Availability.Application.Interfaces
{
    /// <summary>
    /// Aviso de nuevas solicitudes a quien las procesa (broker).
    /// </summary>
    public interface IPublicadorSolicitudes
    {
        Task PublicarAsync(SolicitudReserva solicitud);
    }
}
