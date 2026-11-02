using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditLog.Api.Controllers;

[ApiController]
[Route("session")]
public sealed class SessionController(IConfiguration configuration) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var password = configuration["AuditLog:Password"];
        if (string.IsNullOrWhiteSpace(password))
            return Problem("Configurar AuditLog:Password (AUDITLOG_PASSWORD en Docker Compose).", statusCode: 503);

        if (!CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(request.Password ?? "")),
                SHA256.HashData(Encoding.UTF8.GetBytes(password))))
            return Unauthorized();

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "demo")],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
        return NoContent();
    }

    [Authorize]
    [HttpGet]
    public IActionResult Current() => NoContent();

    [Authorize]
    [HttpDelete]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    public sealed record LoginRequest(string? Password);
}
