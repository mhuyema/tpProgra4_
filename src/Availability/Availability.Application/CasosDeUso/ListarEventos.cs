using Availability.Application.DTOs;
using Availability.Application.Interfaces;

namespace Availability.Application.CasosDeUso
{
    /// <summary>
    /// Caso de uso: ver los eventos disponibles (paso 1, sin login).
    /// Devuelve DTOs para no exponer las entidades de dominio hacia Web.
    /// </summary>
    public class ListarEventos(IEventoRepositorio eventos, IConsultaReservas consultaReservas)
    {
        public async Task<List<EventoDTO>> EjecutarAsync()
        {
            var lista = await eventos.ListarEventosAsync();
            var resultado = new List<EventoDTO>();

            foreach (var e in lista)
            {
                var ocupados = await consultaReservas.ObtenerAsientosOcupadosAsync(e.Id);
                resultado.Add(new EventoDTO(e.Id, e.Nombre, e.Descripcion, e.Lugar, e.FechaInicio, e.FechaFin,
                    e.ImageUrl, Math.Max(0, e.Capacidad - ocupados.Count)));
            }

            return resultado;
        }
    }
}
