using Availability.Application.Interfaces;


namespace Availability.Application.CasosDeUso
{
    /// <summary>
    /// Caso de uso: obtener los IDs de los asientos libres de un evento (paso 1, sin login).
    /// Availability conoce el catálogo; la ocupación es de Booking y se le consulta.
    /// </summary>
    public class ConsultarAsientosLibres(
        IEventoRepositorio eventoRepositorio,
        IAsientoRepositorio asientoRepositorio,
        IConsultaReservas consultaReservas,
        IAuditoriaPublicador auditoria)
    {
        public async Task<List<int>> EjecutarAsync(int eventoId)
        {
            var evento = await eventoRepositorio.ObtenerEventoPorIdAsync(eventoId);
            if (evento is null)
            {
                var ex = new Exception("El evento no existe.");
                await auditoria.EmitirErrorAsync(Guid.NewGuid().ToString(), "ConsultarAsientosLibres", ex,
                    new Dictionary<string, object?> { ["EventoId"] = eventoId });
                throw ex;
            }

            var catalogo = await asientoRepositorio.ListarPorEventoAsync(eventoId);
            var response = await consultaReservas.ObtenerAsientosOcupadosAsync(eventoId);

            var idsOcupados = response.AsientosOcupados
                .ToHashSet();

            return catalogo
                .Select(a => a.Id)
                .Where(id => !idsOcupados.Contains(id))
                .ToList();
        }
    }
}
