using Availability.Application.DTOs;
using Availability.Application.Interfaces;

namespace Availability.Application.CasosDeUso
{
    /// <summary>
    /// Caso de uso: ver el detalle de un evento (resumen de la pantalla de reserva).
    /// Devuelve null si no existe, para que Web decida cómo responder.
    /// </summary>
    public class ObtenerEvento(IEventoRepositorio eventos, IConsultaReservas consultaReservas)
    {
        public async Task<EventoDTO?> EjecutarAsync(int eventoId)
        {
            var e = await eventos.ObtenerEventoPorIdAsync(eventoId);
            if (e is null) return null;

            var ocupados = await consultaReservas.ObtenerAsientosOcupadosAsync(e.Id);
            return new EventoDTO(e.Id, e.Nombre, e.Descripcion, e.Lugar, e.FechaInicio, e.FechaFin,
                e.ImageUrl, Math.Max(0, e.Capacidad - ocupados.Count));
        }
    }
}
