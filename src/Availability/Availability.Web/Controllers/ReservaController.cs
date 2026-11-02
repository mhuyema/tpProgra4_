using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Availability.Application.CasosDeUso;
using Availability.Application.Servicios;
using Availability.Web.Models;
using Availability.Web.Models.Mapping;

namespace Availability.Web.Controllers;

public class ReservaController(
    ObtenerEvento obtenerEvento,
    ConsultarAsientosLibres consultarAsientos,
    ArmadorMapaAsientos armadorMapa,
    SolicitarReserva solicitarReserva,
    ConsultarEstadoSolicitud consultarEstado) : Controller
{
    private const string PendienteKey = "ReservaPendiente";

    [HttpGet]
    public async Task<IActionResult> Asientos(int id)
    {
        var evento = await obtenerEvento.EjecutarAsync(id);
        if (evento is null) return NotFound();

        var idsLibres = await consultarAsientos.EjecutarAsync(id);
        var asientos = await armadorMapa.ArmarAsync(id, idsLibres);
        var autenticado = User.Identity?.IsAuthenticated == true;

        // Si venía de iniciar sesión (o de un intento fallido), se restaura la selección que había hecho.
        IReadOnlyList<int>? preseleccion = null;
        if (LeerPendiente() is { } pendiente && pendiente.EventoId == id)
        {
            preseleccion = pendiente.AsientoIds;
            HttpContext.Session.Remove(PendienteKey);
        }

        return View(new SeatSelectionViewModel(evento.Id, evento.ToSummary(), asientos.ToSeatMap(), autenticado, preseleccion,
            TempData["LoginError"] as string, TempData["LoginEmail"] as string));
    }

    /// <summary>Login desde el panel de la pantalla de asientos: conserva la selección y vuelve a la misma pantalla tras autenticar en el Authorization Server.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult IniciarSesion(IniciarSesionReservaViewModel model)
    {
        if (model.AsientoIds.Count is >= 1 and <= 6)
            GuardarPendiente(new ReservaPendiente(model.EventoId, model.AsientoIds));

        var volver = Url.Action(nameof(Asientos), new { id = model.EventoId })!;
        return Challenge(new AuthenticationProperties { RedirectUri = volver }, OpenIdConnectDefaults.AuthenticationScheme);
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

        // Sin sesión: se guarda la selección y se manda a iniciar sesión, volviendo a esta misma pantalla.
        if (User.Identity?.IsAuthenticated != true)
        {
            GuardarPendiente(new ReservaPendiente(model.EventoId, model.AsientoIds));
            var volver = Url.Action(nameof(Asientos), new { id = model.EventoId });
            return RedirectToAction("Login", "Cuenta", new { returnUrl = volver });
        }

        try
        {
            var solicitudes = await solicitarReserva.EjecutarAsync(
                model.EventoId, model.AsientoIds,
                User.Id(), User.Nombre(), User.Email());

            // RouteValues no serializa colecciones (daría "ids=System.Collections...Guid]"): se arma ids=a&ids=b a mano.
            var query = string.Join("&", solicitudes.Select(s => $"ids={s}"));
            return Redirect($"{Url.Action(nameof(Estado), new { eventoId = model.EventoId })}&{query}");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Asientos), new { id = model.EventoId });
        }
    }

    [Authorize, HttpGet]
    public async Task<IActionResult> Estado(int eventoId, Guid[] ids)
    {
        var evento = await obtenerEvento.EjecutarAsync(eventoId);
        if (evento is null || ids.Length == 0) return NotFound();

        var items = new List<SolicitudEstadoItem>();
        try
        {
            foreach (var id in ids)
            {
                var estado = await consultarEstado.EjecutarAsync(id, User.Id());
                items.Add(new SolicitudEstadoItem(estado.SolicitudId, estado.Estado, estado.Motivo, estado.Monto));
            }
        }
        catch (Exception)
        {
            return NotFound();
        }

        return View(new EstadoSolicitudViewModel(evento.Id, evento.ToSummary(), items));
    }

    private void GuardarPendiente(ReservaPendiente pendiente) =>
        HttpContext.Session.SetString(PendienteKey, JsonSerializer.Serialize(pendiente));

    private ReservaPendiente? LeerPendiente()
    {
        var json = HttpContext.Session.GetString(PendienteKey);
        return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<ReservaPendiente>(json);
    }
}
