using Availability.Domain.Entities;

namespace Availability.Application.Interfaces
{
    /// <summary>
    /// Aviso de nuevas solicitudes a quien las procesa (broker).
    /// </summary>
    public interface IPublicadorSolicitudes
    {
        /// <param name="accessToken">Token del usuario; viaja en el mensaje para que Booking verifique la identidad. No se persiste.</param>
        Task PublicarAsync(SolicitudReserva solicitud, string accessToken);
    }
}
