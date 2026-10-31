using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Availability.Web;

/// <summary>
/// Datos del usuario autenticado y alta de la sesión. Es el único lugar a reemplazar
/// por autenticación real cuando existan las cuentas.
/// </summary>
public static class UsuarioActual
{
    public static string Id(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    public static string Nombre(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.Name) ?? "";
    public static string Email(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.Email) ?? "";

    /// <summary>Inicia sesión por cookie. Autenticación simulada: acepta cualquier email válido.</summary>
    public static Task IniciarSesionAsync(this HttpContext context, string email)
    {
        email = email.Trim().ToLowerInvariant();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, email),
            new(ClaimTypes.Name, email.Split('@')[0]),
            new(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }
}
