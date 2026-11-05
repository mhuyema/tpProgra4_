using Microsoft.AspNetCore.Mvc;
using Booking.API.Auth;
using Booking.Business.Services;
using Booking.API.DTOs;

namespace Booking.API.Controllers;

[ApiController]
[Route("api/reservas")]
public class ReservasController : ControllerBase
{
    private readonly IReservaService _reservaService;

    public ReservasController(IReservaService reservaService)
    {
        _reservaService = reservaService;
    }

    // Permite a Availability consultar el estado de una solicitud por RequestId.
    // Exige el token del USUARIO logueado (Availability lo reenvía) y además comprueba que la reserva sea suya:
    // sin esto, cualquiera que conociera un RequestId podría ver la reserva de otra persona (punto 3.e).
    [RequiereToken(TipoLlamador.Usuario)]
    [HttpGet("solicitudes/{requestId:guid}")]
    public async Task<ActionResult<EstadoSolicitudResponse>> ObtenerEstadoSolicitud(Guid requestId)
    {
        // Lo dejó el filtro AutenticacionRestFilter después de validar el token con el Authorization Server.
        var usuarioId = HttpContext.Items[AutenticacionRestFilter.SubjectItemKey] as string;

        var reserva = await _reservaService.ObtenerPorSolicitudAsync(requestId);

        // Si la reserva es de otra persona respondemos 404 (igual que si no existiera) para no revelar
        // que ese RequestId existe. Availability ya interpreta 404 como "Pendiente".
        if (reserva is null || !string.Equals(reserva.UsuarioId, usuarioId, StringComparison.Ordinal))
            return NotFound();

        var respuesta = new EstadoSolicitudResponse(
            RequestId: reserva.SolicitudId,
            Status: reserva.Estado.ToString(),
            RejectionReason: reserva.Motivo?.ToString(),
            ExpiresAt: reserva.VenceEn
        );

        return Ok(respuesta);
    }

    // Permite a Availability consultar los asientos ocupados de un evento.
    // El filtrado se realiza en Booking, incluyendo reservas Confirmadas
    // y Retenidas cuyo plazo todavía está vigente.
    // Devuelve únicamente los identificadores de los asientos ocupados.
    // La pantalla de asientos es pública (puede verla alguien sin sesión), así que acá NO se exige un usuario:
    // se exige que llame el SERVICIO Availability con su propio token (client credentials). De esta forma
    // nadie puede consultar Booking directamente; solo Availability, autenticado ante el Authorization Server.
    [RequiereToken(TipoLlamador.ServicioAvailability)]
    [HttpGet("eventos/{eventoId:int}/asientos-ocupados")]
    public async Task<ActionResult<AsientosOcupadosResponseDTO>> ObtenerAsientosOcupados(int eventoId)
    {
        var asientosOcupados = await _reservaService
            .ObtenerAsientosOcupadosPorEventoAsync(eventoId);

        var respuesta = new AsientosOcupadosResponseDTO
        {
            EventoId = eventoId,
            AsientosOcupados = asientosOcupados.ToList()
        };

        return Ok(respuesta);
    }
}