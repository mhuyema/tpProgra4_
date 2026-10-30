using System.ComponentModel.DataAnnotations;

namespace Availability.Web.Models;

public class ConfirmarReservaViewModel
{
    [Required]
    public int EventoId { get; set; }

    [Required(ErrorMessage = "Elegí al menos un asiento.")]
    [MinLength(1, ErrorMessage = "Elegí al menos un asiento.")]
    [MaxLength(6, ErrorMessage = "Podés reservar hasta 6 asientos.")]
    public List<int> AsientoIds { get; set; } = new();
}
