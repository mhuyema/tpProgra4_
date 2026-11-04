using Authorization.Server;
using Authorization.Server.Data;
using Authorization.Server.Models;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using System.Security.Cryptography.X509Certificates;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);
var serverOptions = AuthorizationServerOptions.Load(builder.Configuration, builder.Environment);

X509Certificate2? signingCertificate = null;
X509Certificate2? encryptionCertificate = null;
if (builder.Environment.IsProduction())
{
    signingCertificate = new X509Certificate2(
        serverOptions.SigningCertificatePath!,
        serverOptions.SigningCertificatePassword,
        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);
    encryptionCertificate = new X509Certificate2(
        serverOptions.EncryptionCertificatePath!,
        serverOptions.EncryptionCertificatePassword,
        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton(serverOptions);

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<Authorization.Server.Services.UserRegistrationService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
})
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    options.ClientId = serverOptions.GoogleClientId;
    options.ClientSecret = serverOptions.GoogleClientSecret;
    options.CallbackPath = serverOptions.GoogleCallbackPath;
    options.SignInScheme = IdentityConstants.ExternalScheme;
});
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/auth/login";
    // .NET 10 responde 401 (en vez de redirigir) en endpoints [ApiController]; /connect/authorize necesita el redirect al login.
    options.Events.OnRedirectToLogin = ctx =>
    {
        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
    options.Cookie.Name = "authorization.server.session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
});

builder.Services.AddOpenIddict()
    .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>())
    .AddServer(options =>
    {
        options.SetIssuer(new Uri(serverOptions.Issuer))
            .SetAccessTokenLifetime(TimeSpan.FromMinutes(15))
            .SetRefreshTokenLifetime(TimeSpan.FromDays(14));
        options.SetAuthorizationEndpointUris("/connect/authorize")
            .SetTokenEndpointUris("/connect/token")
            .SetEndSessionEndpointUris("/connect/logout");

        options.AllowAuthorizationCodeFlow().AllowRefreshTokenFlow();
        options.RegisterScopes(Scopes.OpenId, Scopes.Email, Scopes.Profile, serverOptions.Audience);
        if (builder.Environment.IsProduction())
            options.AddEncryptionCertificate(encryptionCertificate!).AddSigningCertificate(signingCertificate!);
        else
            options.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();
        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough()
            .EnableEndSessionEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.SetIssuer(new Uri(serverOptions.Issuer));
        options.AddAudiences(serverOptions.Audience);
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (app.Environment.IsDevelopment())
        await db.Database.EnsureCreatedAsync();

    await SeedData.InitializeAsync(scope.ServiceProvider, serverOptions);
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
