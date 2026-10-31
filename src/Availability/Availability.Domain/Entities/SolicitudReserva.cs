using Availability.Domain.Entities.Core;

namespace Availability.Domain.Entities
{
    /// <summary>
    /// Solicitud de reserva de un asiento, registrada antes de publicarse en el broker.
    /// No guarda estado ni vencimiento.
    /// </summary>
    public class SolicitudReserva
    {
        public Guid RequestId { get; set; }
        
        public int EventoId { get; set; }
        public int AsientoId { get; set; }

        public string UsuarioId { get; set; } = string.Empty;

        public decimal Monto { get; set; }
    }
}
