using Microsoft.AspNetCore.Mvc;

namespace Availability.Web.Controllers;

public class StyleguideController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
