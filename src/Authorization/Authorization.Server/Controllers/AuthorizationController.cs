using System.Security.Claims;
using Authorization.Server.Models;
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
    AuthorizationServerOptions serverOptions) : Controller
{
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
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = result.Principal.FindFirstValue(ClaimTypes.Name) };
            var created = await userManager.CreateAsync(user);
            if (!created.Succeeded) return BadRequest(created.Errors);
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
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
