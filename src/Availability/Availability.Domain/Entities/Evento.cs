using Availability.Domain.Entities.Core;


namespace Availability.Domain.Entities
{
    /// <summary>
    /// Evento con asientos para reservar. Entidad anémica: solo datos.
    /// </summary>
    public class Evento : EntidadBase
    {
        public string Nombre { get; set; }

     
        public string Descripcion { get; set; }

        public string Lugar { get; set; }

        public DateTime FechaInicio { get; set; }

      
        public DateTime FechaFin { get; set; }

        public int Capacidad { get; set; }

        public string? ImageUrl { get; set; }

        public List<Asiento> Asientos { get; set; } = new List<Asiento>();

   
    }
}
