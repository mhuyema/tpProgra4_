using Authorization.Server.Models;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Authorization.Server;

namespace Authorization.Server.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services, AuthorizationServerOptions options)
    {
        await SeedUsuarioDePruebaAsync(services);

        var manager = services.GetRequiredService<IOpenIddictApplicationManager>();
        const string clientId = "availability-web";

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            DisplayName = "Availability web",
            ClientType = ClientTypes.Public,
            ConsentType = ConsentTypes.Implicit,
            RedirectUris = { new Uri(options.ClientRedirectUri) },
            PostLogoutRedirectUris = { new Uri(new Uri(options.ClientRedirectUri), "/signout-callback-oidc") },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.EndSession,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Prefixes.Scope + Scopes.OpenId,
                Permissions.Prefixes.Scope + Scopes.Email,
                Permissions.Prefixes.Scope + Scopes.Profile,
                Permissions.Prefixes.Scope + options.Audience
            },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange }
        };

        await UpsertAsync(manager, descriptor);
        await SeedBookingServiceAsync(manager, options);
    }

    // Cliente confidencial de Booking: pide un token de servicio (client credentials) para llamar a Payment.
    private static async Task SeedBookingServiceAsync(IOpenIddictApplicationManager manager, AuthorizationServerOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BookingServiceClientSecret)) return;

        await UpsertAsync(manager, new OpenIddictApplicationDescriptor
        {
            ClientId = "booking-service",
            ClientSecret = options.BookingServiceClientSecret,
            DisplayName = "Booking (servicio)",
            ClientType = ClientTypes.Confidential,
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.ClientCredentials
            }
        });
    }

    private static async Task UpsertAsync(IOpenIddictApplicationManager manager, OpenIddictApplicationDescriptor descriptor)
    {
        var application = await manager.FindByClientIdAsync(descriptor.ClientId!);
        if (application is null)
            await manager.CreateAsync(descriptor);
        else
            await manager.UpdateAsync(application, descriptor);
    }

    private static async Task SeedUsuarioDePruebaAsync(IServiceProvider services)
    {
        const string email = "test@bombo.com";
        const string password = "Test1234!";

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(email) is not null) return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = "Usuario de Prueba"
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                "No se pudo crear el usuario de prueba: " + string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
