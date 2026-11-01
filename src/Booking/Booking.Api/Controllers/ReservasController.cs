
using Microsoft.AspNetCore.Mvc;
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
    [HttpGet("solicitudes/{requestId:guid}")]
    public async Task<ActionResult<EstadoSolicitudResponse>> ObtenerEstadoSolicitud(Guid requestId)
    {
        var reserva = await _reservaService.ObtenerPorSolicitudAsync(requestId);

        if (reserva is null)
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
