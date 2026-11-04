using System.Security.Claims;

namespace Availability.Web;

/// <summary>
/// Datos del usuario autenticado, tomados de los claims que emite el Authorization Server (sub, name, email).
/// </summary>
public static class UsuarioActual
{
    public static string Id(this ClaimsPrincipal user) => user.FindFirstValue("sub") ?? "";
    public static string Nombre(this ClaimsPrincipal user) => user.FindFirstValue("name") ?? "";
    public static string Email(this ClaimsPrincipal user) => user.FindFirstValue("email") ?? "";
}
