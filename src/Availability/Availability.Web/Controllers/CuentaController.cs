using Microsoft.AspNetCore.Mvc;
using Availability.Web.Models;

namespace Availability.Web.Controllers;

public class CuentaController : Controller
{
    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // Pendiente: autenticar con Application/Infrastructure cuando existan.
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
