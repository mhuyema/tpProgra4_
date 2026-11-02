using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Availability.Web.Models;

namespace Availability.Web.Controllers;

public class CuentaController : Controller
{
    /// <summary>Delega el login al Authorization Server (la pantalla de credenciales es la del servidor).</summary>
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        var destino = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Index", "Home")!;
        return Challenge(new AuthenticationProperties { RedirectUri = destino }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove("ReservaPendiente");

        // Cierra la cookie local y la sesión en el Authorization Server; este vuelve a /signout-callback-oidc y de ahí al inicio.
        return SignOut(
            new AuthenticationProperties { RedirectUri = Url.Action("Index", "Home") },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // Pendiente: crear la cuenta con Application/Infrastructure cuando existan.
        return RedirectToAction("Login");
    }
}
