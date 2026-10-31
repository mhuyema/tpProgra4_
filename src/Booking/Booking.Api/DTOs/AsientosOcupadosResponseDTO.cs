
namespace Booking.API.DTOs;

// Respuesta del endpoint de consulta de asientos ocupados.
// Incluye el evento consultado y los asientos filtrados por Booking.
public record AsientosOcupadosResponseDTO
{
    public int EventoId { get; set; }

    // Devuelve únicamente los identificadores de los asientos ocupados.
    // Si no existen reservas que ocupen asientos,
    // la colección se devuelve vacía.
    public List<int> AsientosOcupados { get; set; } = new();
}
