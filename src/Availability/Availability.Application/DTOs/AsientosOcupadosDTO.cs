namespace Availability.Application.DTOs
{

    public record AsientosOcupadosDTO
    {
        public int AsientoId { get; set; }
        
        public string Estadoinfo { get; set; } = string.Empty;
    }

    public record AsientosOcupadosResponseDTO
    {
        public int EventoId { get; set; }
        public List<AsientosOcupadosDTO> AsientosOcupados { get; set; } = new List<AsientosOcupadosDTO>();
    }
}
