using Microsoft.AspNetCore.Mvc;
using Availability.Web.Models;

namespace Availability.Web.Controllers;

public class CuentaController : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // Pendiente: validar credenciales con Application/Infrastructure cuando existan.
        await HttpContext.IniciarSesionAsync(model.Email);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
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
