using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Authorization.Server;

namespace Authorization.Server.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services, AuthorizationServerOptions options)
    {
        var manager = services.GetRequiredService<IOpenIddictApplicationManager>();
        const string clientId = "availability-web";

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            DisplayName = "Availability web",
            ClientType = ClientTypes.Public,
            ConsentType = ConsentTypes.Implicit,
            RedirectUris = { new Uri(options.ClientRedirectUri) },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
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

        var application = await manager.FindByClientIdAsync(clientId);
        if (application is null)
            await manager.CreateAsync(descriptor);
        else
            await manager.UpdateAsync(application, descriptor);
    }
}
