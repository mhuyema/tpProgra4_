namespace Availability.Application.DTOs
{

    public record AsientosOcupadosResponseDTO
    {
        public int EventoId { get; set; }
        public List<int> AsientosOcupados { get; set; } = new List<int>();
    }
}
