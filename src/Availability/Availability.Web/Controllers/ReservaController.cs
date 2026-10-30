using Microsoft.AspNetCore.Mvc;
using Availability.Application.CasosDeUso;
using Availability.Web.Models;
using Availability.Web.Models.Mapping;

namespace Availability.Web.Controllers;

public class ReservaController(
    ObtenerEvento obtenerEvento,
    ConsultarAsientosLibres consultarAsientos,
    SolicitarReserva solicitarReserva,
    ConsultarEstadoSolicitud consultarEstado) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Asientos(int id)
    {
        var evento = await obtenerEvento.EjecutarAsync(id);
        if (evento is null) return NotFound();

        var asientos = await consultarAsientos.EjecutarAsync(id);

        return View(new SeatSelectionViewModel(evento.Id, evento.ToSummary(), asientos.ToSeatMap()));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirmar(ConfirmarReservaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = ModelState.Values
                .SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault(m => !string.IsNullOrEmpty(m))
                ?? "No pudimos procesar la selección.";
            return RedirectToAction(nameof(Asientos), new { id = model.EventoId });
        }

        try
        {
            var solicitudes = await solicitarReserva.EjecutarAsync(
                model.EventoId, model.AsientoIds,
                UsuarioDePrueba.Id, UsuarioDePrueba.Nombre, UsuarioDePrueba.Email);

            return RedirectToAction(nameof(Estado), new { eventoId = model.EventoId, ids = solicitudes });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Asientos), new { id = model.EventoId });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Estado(int eventoId, Guid[] ids)
    {
        var evento = await obtenerEvento.EjecutarAsync(eventoId);
        if (evento is null || ids.Length == 0) return NotFound();

        var items = new List<SolicitudEstadoItem>();
        try
        {
            foreach (var id in ids)
            {
                var estado = await consultarEstado.EjecutarAsync(id, UsuarioDePrueba.Id);
                items.Add(new SolicitudEstadoItem(estado.SolicitudId, estado.Estado, estado.Motivo));
            }
        }
        catch (Exception)
        {
            return NotFound();
        }

        return View(new EstadoSolicitudViewModel(evento.Id, evento.ToSummary(), items));
    }
}
