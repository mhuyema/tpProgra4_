using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Availability.Application.CasosDeUso;
using Availability.Web.Models;
using Availability.Web.Models.Components;
using Availability.Web.Models.Mapping;
namespace Availability.Web.Controllers;

public class HomeController(ListarEventos listarEventos) : Controller
{
    public async Task<IActionResult> Index()
    {
        var eventos = await listarEventos.EjecutarAsync();
        var proximo = eventos.FirstOrDefault();

        // Hero, cartelera y cards de eventos vienen de Application; filtros y venues siguen de muestra.
        var hero = proximo?.ToHero()
            ?? new HeroModel("Próximamente", "", "Todavía no hay eventos publicados.", ViewModelMapper.HeroImage, "Ver cartelera", "#");

        var model = new HomeViewModel(
            hero,
            new List<ChipModel>
            {
                new("Todos", true),
                new("Este finde"),
                new("Open air"),
                new("Techno"),
                new("House"),
                new("After")
            },
            eventos.Select(e => e.ToCard()).ToList(),
            new List<VenueCardModel>
            {
                new("La Fábrica", "Barracas, CABA", ImagePlaceholder.Gradient("353534", "0e0e0e"), "#"),
                new("Hangar 7", "Puerto Madero, CABA", ImagePlaceholder.Gradient("2a2a2a", "131313"), "#"),
                new("El Silo", "Rosario", ImagePlaceholder.Gradient("c6c6c7", "1c1b1b"), "#")
            });

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
