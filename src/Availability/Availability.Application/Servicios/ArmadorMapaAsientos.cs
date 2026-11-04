using Availability.Application.DTOs;
using Availability.Application.Interfaces;

namespace Availability.Application.Servicios
{
    /// <summary>
    /// Auxiliar de ConsultarAsientosLibres: a partir de los IDs libres, completa fila, número y precio
    /// con el catálogo propio y devuelve todos los asientos del evento con su estado, listos para la vista.
    /// </summary>
    public class ArmadorMapaAsientos(IAsientoRepositorio asientoRepositorio)
    {
        public async Task<List<AsientoDTO>> ArmarAsync(int eventoId, IReadOnlyCollection<int> idsLibres)
        {
            var libres = idsLibres.ToHashSet();
            var catalogo = await asientoRepositorio.ListarPorEventoAsync(eventoId);

            return catalogo
                .Select(a => new AsientoDTO(a.Id, a.Fila, a.Numero, a.Precio, Ocupado: !libres.Contains(a.Id)))
                .ToList();
        }
    }
}
