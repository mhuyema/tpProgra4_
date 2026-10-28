using Availability.Application.DTOs;
using Availability.Application.Interfaces;


namespace Availability.Application.CasosDeUso
{
    /// <summary>
    /// Caso de uso: ver los asientos de un evento, marcando cuáles están ocupados (paso 1, sin login).
    /// Availability conoce el catálogo; la ocupación es de Booking y se le consulta.
    /// </summary>
    public class ConsultarAsientosLibres(IEventoRepositorio eventoRepositorio, IAsientoRepositorio asientoRepositorio, IConsultaReservas consultaReservas) 
    {
        public async Task<List<AsientoDTO>> EjecutarAsync(Guid eventoId)
        {
            _ = await eventoRepositorio.ObtenerEventoPorIdAsync(eventoId)
                ?? throw new Exception("El evento no existe.");

            var todos = await asientoRepositorio.ListarPorEventoAsync(eventoId);
            var ocupados = await consultaReservas.ObtenerAsientosOcupadosAsync(eventoId);

            return todos
                .Select(a => new AsientoDTO(a.Id, a.Fila, a.Numero, a.Precio, ocupados.Contains(a.Id)))
                .ToList();
        }

    }
}
