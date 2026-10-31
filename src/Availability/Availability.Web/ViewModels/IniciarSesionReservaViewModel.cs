using System.ComponentModel.DataAnnotations;

namespace Availability.Web.Models;

/// <summary>Login hecho desde el panel de la pantalla de asientos: credenciales + la selección a conservar.</summary>
public class IniciarSesionReservaViewModel : ConfirmarReservaViewModel
{
    [Required(ErrorMessage = "Ingresá tu email.")]
    [EmailAddress(ErrorMessage = "El email no es válido.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Ingresá tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = "";
}
