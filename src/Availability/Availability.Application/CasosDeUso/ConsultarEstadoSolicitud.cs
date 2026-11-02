using Availability.Application.DTOs;
using Availability.Application.Interfaces;

namespace Availability.Application.CasosDeUso
{
    /// <summary>
    /// Caso de uso: ver cómo va una reserva (paso 7). El estado no se guarda en Availability:
    /// es de Booking y se le consulta en cada pedido.
    /// </summary>
    public class ConsultarEstadoSolicitud(
        ISolicitudReservaRepositorio solicitudes,
        IConsultaReservas consultaReservas)
    {
        public async Task<EstadoSolicitudDTO> EjecutarAsync(Guid solicitudId, string usuarioId)
        {
            var solicitud = await solicitudes.ObtenerPorIdAsync(solicitudId);

            if (solicitud is null || solicitud.UsuarioId != usuarioId)
                throw new Exception("La solicitud no existe.");

            // El monto lo conoce Availability (se fijó al crear la solicitud); el estado, Booking.
            var estado = await consultaReservas.ObtenerEstadoAsync(solicitudId);
            return estado with { Monto = solicitud.Monto };
        }
    }
    }
