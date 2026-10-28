using Availability.Domain.Entities.Core;

namespace Availability.Domain.Entities
{
    /// <summary>
    /// Solicitud de reserva de un asiento, registrada antes de publicarse en el broker.
    /// No guarda estado ni vencimiento.
    /// </summary>
    public class SolicitudReserva : EntidadBase
    {
        public Guid AsientoId { get; set; }
        public Asiento? Asiento { get; set; }

        public string UsuarioId { get; set; } = string.Empty;
        public string NombreCliente { get; set; } = string.Empty;
        public string EmailCliente { get; set; } = string.Empty;
      
    }
}
