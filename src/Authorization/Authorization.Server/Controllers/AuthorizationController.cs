using System.Security.Claims;
using Authorization.Server.Auditing;
using Authorization.Server.Models;
using Authorization.Server.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Authorization.Server.Controllers;

[ApiController]
public sealed class AuthorizationController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    UserRegistrationService registration,
    AuthorizationServerOptions serverOptions,
    IAuthAuditPublisher audit) : Controller
{
    [HttpGet("/auth/register")]
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null) =>
        View(new RegisterViewModel { ReturnUrl = returnUrl });

    [HttpPost("/auth/register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromForm] string? name, [FromForm] string? email, [FromForm] string? password,
        [FromForm] string? confirmPassword, [FromForm] string? returnUrl = null)
    {
        var correlationId = Guid.NewGuid().ToString();
        const string processName = "RegistrarUsuario";
        var details = new Dictionary<string, object?> { ["Email"] = email };
        await audit.InitProcessAsync(correlationId, processName, details);

        try
        {
            async Task<IActionResult> Fail(string error)
            {
                await audit.EndProcessAsync(correlationId, processName, $"Rechazado: {error}", details);
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return View(new RegisterViewModel { Name = name, Email = email, ReturnUrl = returnUrl, Error = error });
            }

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
                return await Fail("Completá todos los campos.");
            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
                return await Fail("El email no es válido.");
            if (password != confirmPassword)
                return await Fail("Las contraseñas no coinciden.");
            if (await userManager.FindByEmailAsync(email) is not null)
                return await Fail("Ya existe una cuenta con ese email.");

            var (created, user) = await registration.RegisterAsync(email.Trim(), name, password);
            if (!created.Succeeded)
                return await Fail(string.Join(" ", created.Errors.Select(e => e.Description)));

            await signInManager.SignInAsync(user!, isPersistent: false);
            details["UserId"] = user!.Id;
            await audit.EndProcessAsync(correlationId, processName, "UsuarioRegistrado", details);
            return Redirect(!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
        }
        catch (Exception exception)
        {
            await audit.EmitErrorAsync(correlationId, processName, exception, details);
            throw;
        }
    }

    [HttpGet("/auth/login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost("/auth/login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromForm] string email, [FromForm] string password, [FromForm] string? returnUrl = null)
    {
        var correlationId = Guid.NewGuid().ToString();
        const string processName = "IniciarSesion";
        var details = new Dictionary<string, object?> { ["Email"] = email };
        await audit.InitProcessAsync(correlationId, processName, details);

        try
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null || !await userManager.CheckPasswordAsync(user, password))
            {
                await audit.EndProcessAsync(correlationId, processName, "CredencialesInvalidas", details);
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                return View(new LoginViewModel { Email = email, ReturnUrl = returnUrl, Error = "Credenciales inválidas." });
            }

            await signInManager.SignInAsync(user, isPersistent: false);
            details["UserId"] = user.Id;
            await audit.EndProcessAsync(correlationId, processName, "SesionIniciada", details);
            return Redirect(!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
        }
        catch (Exception exception)
        {
            await audit.EmitErrorAsync(correlationId, processName, exception, details);
            throw;
        }
    }

    [HttpGet("/auth/google")]
    [AllowAnonymous]
    public IActionResult Google(string? returnUrl = null) =>
        Challenge(new AuthenticationProperties { RedirectUri = $"/auth/google-callback?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}" }, GoogleDefaults.AuthenticationScheme);

    [HttpGet("/auth/google-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
    {
        var correlationId = Guid.NewGuid().ToString();
        const string processName = "IniciarSesionGoogle";
        var details = new Dictionary<string, object?>();
        await audit.InitProcessAsync(correlationId, processName, details);

        try
        {
            var result = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
            if (!result.Succeeded || result.Principal is null)
            {
                await audit.EndProcessAsync(correlationId, processName, "GoogleNoAutentico", details);
                return Unauthorized("No se pudo autenticar con Google.");
            }

            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            details["Email"] = email;
            if (string.IsNullOrWhiteSpace(email))
            {
                await audit.EndProcessAsync(correlationId, processName, "GoogleSinEmail", details);
                return Unauthorized("Google no devolvió un email.");
            }

            var user = await userManager.FindByEmailAsync(email);
            details["CuentaNueva"] = user is null;
            if (user is null)
            {
                var (created, newUser) = await registration.RegisterAsync(email, result.Principal.FindFirstValue(ClaimTypes.Name), password: null);
                if (!created.Succeeded)
                {
                    await audit.EndProcessAsync(correlationId, processName, "RegistroFallido", details);
                    return BadRequest(created.Errors);
                }
                user = newUser!;
            }

            await signInManager.SignInAsync(user, isPersistent: false);
            details["UserId"] = user.Id;
            await audit.EndProcessAsync(correlationId, processName, "SesionIniciada", details);
            return Redirect(!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
        }
        catch (Exception exception)
        {
            await audit.EmitErrorAsync(correlationId, processName, exception, details);
            throw;
        }
    }

    [HttpGet("/connect/authorize")]
    [HttpPost("/connect/authorize")]
    [AllowAnonymous]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("Solicitud OAuth inválida.");
        if (User.Identity?.IsAuthenticated != true)
        {
            var target = Request.PathBase + Request.Path + Request.QueryString;
            return Challenge(new AuthenticationProperties { RedirectUri = target }, IdentityConstants.ApplicationScheme);
        }

        var user = await userManager.GetUserAsync(User) ?? throw new InvalidOperationException("Usuario no encontrado.");
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, Claims.Name, Claims.Role);
        identity.AddClaim(new Claim(Claims.Subject, await userManager.GetUserIdAsync(user)));
        identity.AddClaim(new Claim(Claims.Name, user.UserName ?? user.Email ?? string.Empty));
        identity.AddClaim(new Claim(Claims.Email, user.Email ?? string.Empty));
        identity.SetScopes(request.GetScopes());
        identity.SetResources(serverOptions.Audience);
        identity.SetDestinations(static claim => claim.Type switch
        {
            Claims.Name or Claims.Email => new[] { Destinations.AccessToken, Destinations.IdentityToken },
            _ => new[] { Destinations.AccessToken, Destinations.IdentityToken }
        });
        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("/connect/token")]
    [AllowAnonymous]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("Solicitud OAuth inválida.");

        var correlationId = Guid.NewGuid().ToString();
        const string processName = "EmitirToken";
        var details = new Dictionary<string, object?>
        {
            ["GrantType"] = request.GrantType,
            ["ClientId"] = request.ClientId
        };
        await audit.InitProcessAsync(correlationId, processName, details);

        try
        {
            // Servicio a servicio (Booking -> Payment): no hay usuario, el sujeto es el propio cliente.
            // OpenIddict ya autenticó client_id/client_secret antes de llegar acá.
            if (request.IsClientCredentialsGrantType())
            {
                var servicio = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, Claims.Name, Claims.Role);
                servicio.AddClaim(new Claim(Claims.Subject, request.ClientId ?? throw new InvalidOperationException("Falta client_id.")));
                servicio.SetResources(serverOptions.Audience);
                servicio.SetDestinations(static _ => new[] { Destinations.AccessToken });
                await audit.EndProcessAsync(correlationId, processName, "TokenEmitido", details);
                return SignIn(new ClaimsPrincipal(servicio), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (!result.Succeeded || result.Principal is null)
            {
                await audit.EndProcessAsync(correlationId, processName, "TokenDenegado", details);
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            details["UserId"] = result.Principal.FindFirstValue(Claims.Subject);
            await audit.EndProcessAsync(correlationId, processName, "TokenEmitido", details);
            return SignIn(result.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        catch (Exception exception)
        {
            await audit.EmitErrorAsync(correlationId, processName, exception, details);
            throw;
        }
    }

    [HttpGet("/connect/logout")]
    [HttpPost("/connect/logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        var correlationId = Guid.NewGuid().ToString();
        const string processName = "CerrarSesion";
        var details = new Dictionary<string, object?> { ["UserId"] = userManager.GetUserId(User) };
        await audit.InitProcessAsync(correlationId, processName, details);

        try
        {
            await signInManager.SignOutAsync();
            await audit.EndProcessAsync(correlationId, processName, "SesionCerrada", details);

            // Cierre de sesión OIDC: OpenIddict valida post_logout_redirect_uri y redirige de vuelta al cliente.
            if (HttpContext.GetOpenIddictServerRequest() is not null)
                return SignOut(new AuthenticationProperties { RedirectUri = "/" }, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            return Ok(new { message = "Sesión cerrada." });
        }
        catch (Exception exception)
        {
            await audit.EmitErrorAsync(correlationId, processName, exception, details);
            throw;
        }
    }

    [HttpGet("/api/auth/validate")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public IActionResult Validate() => Ok(new { valid = true, subject = User.FindFirstValue(Claims.Subject), email = User.FindFirstValue(Claims.Email) });
}
