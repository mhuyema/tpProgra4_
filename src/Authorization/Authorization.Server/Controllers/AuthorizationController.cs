using System.Security.Claims;
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
    AuthorizationServerOptions serverOptions) : Controller
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
        IActionResult Fail(string error)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View(new RegisterViewModel { Name = name, Email = email, ReturnUrl = returnUrl, Error = error });
        }

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            return Fail("Completá todos los campos.");
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            return Fail("El email no es válido.");
        if (password != confirmPassword)
            return Fail("Las contraseñas no coinciden.");
        if (await userManager.FindByEmailAsync(email) is not null)
            return Fail("Ya existe una cuenta con ese email.");

        var (created, user) = await registration.RegisterAsync(email.Trim(), name, password);
        if (!created.Succeeded)
            return Fail(string.Join(" ", created.Errors.Select(e => e.Description)));

        await signInManager.SignInAsync(user!, isPersistent: false);
        return Redirect(!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
    }

    [HttpGet("/auth/login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost("/auth/login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromForm] string email, [FromForm] string password, [FromForm] string? returnUrl = null)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !await userManager.CheckPasswordAsync(user, password))
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return View(new LoginViewModel { Email = email, ReturnUrl = returnUrl, Error = "Credenciales inválidas." });
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return Redirect(!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
    }

    [HttpGet("/auth/google")]
    [AllowAnonymous]
    public IActionResult Google(string? returnUrl = null) =>
        Challenge(new AuthenticationProperties { RedirectUri = $"/auth/google-callback?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}" }, GoogleDefaults.AuthenticationScheme);

    [HttpGet("/auth/google-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
    {
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
        if (!result.Succeeded || result.Principal is null) return Unauthorized("No se pudo autenticar con Google.");

        var email = result.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email)) return Unauthorized("Google no devolvió un email.");

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            var (created, newUser) = await registration.RegisterAsync(email, result.Principal.FindFirstValue(ClaimTypes.Name), password: null);
            if (!created.Succeeded) return BadRequest(created.Errors);
            user = newUser!;
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return Redirect(!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
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

        // Servicio a servicio (Booking -> Payment): no hay usuario, el sujeto es el propio cliente.
        // OpenIddict ya autenticó client_id/client_secret antes de llegar acá.
        if (request.IsClientCredentialsGrantType())
        {
            var servicio = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, Claims.Name, Claims.Role);
            servicio.AddClaim(new Claim(Claims.Subject, request.ClientId ?? throw new InvalidOperationException("Falta client_id.")));
            servicio.SetResources(serverOptions.Audience);
            servicio.SetDestinations(static _ => new[] { Destinations.AccessToken });
            return SignIn(new ClaimsPrincipal(servicio), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (!result.Succeeded || result.Principal is null) return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        return SignIn(result.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpGet("/connect/logout")]
    [HttpPost("/connect/logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();

        // Cierre de sesión OIDC: OpenIddict valida post_logout_redirect_uri y redirige de vuelta al cliente.
        if (HttpContext.GetOpenIddictServerRequest() is not null)
            return SignOut(new AuthenticationProperties { RedirectUri = "/" }, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        return Ok(new { message = "Sesión cerrada." });
    }

    [HttpGet("/api/auth/validate")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public IActionResult Validate() => Ok(new { valid = true, subject = User.FindFirstValue(Claims.Subject), email = User.FindFirstValue(Claims.Email) });
}
