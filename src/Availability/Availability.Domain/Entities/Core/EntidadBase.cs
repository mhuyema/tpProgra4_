using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Availability.Domain.Entities.Core
{
    /// <summary>
    /// Entidad genérica base de la que heredan todas las entidades del dominio.
    /// Clase plana: la persistencia y la concurrencia se configuran en Infrastructure.
    /// </summary>
    public class EntidadBase
    {
        public Guid Id { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;

        public byte[] RowVersion { get; set; }
    }
}
