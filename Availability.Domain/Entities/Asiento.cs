using Availability.Domain.Entities.Core;

namespace Availability.Domain.Entities
{
    /// <summary>
    /// Asientos que pertenecen a un evento. Entidad anémica: solo datos.
    /// </summary>
    public class Asiento : EntidadBase
    {
        public Guid EventoId { get; set; }

        public Evento? Evento { get; set; }

        public string Fila { get; set; } = string.Empty;

        public int Numero { get; set; }

        public decimal Precio { get; set; }
    }
}
