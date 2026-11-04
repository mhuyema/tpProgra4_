using Authorization.Server.Models;
using Microsoft.AspNetCore.Identity;

namespace Authorization.Server.Services;

/// <summary>Alta única de usuarios: la usan tanto /auth/register como el login con Google.</summary>
public sealed class UserRegistrationService(UserManager<ApplicationUser> userManager)
{
    /// <param name="password">Null para cuentas creadas vía proveedor externo (Google).</param>
    public async Task<(IdentityResult Result, ApplicationUser? User)> RegisterAsync(string email, string? displayName, string? password)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim()
        };

        var result = string.IsNullOrEmpty(password)
            ? await userManager.CreateAsync(user)
            : await userManager.CreateAsync(user, password);

        return (result, result.Succeeded ? user : null);
    }
}
