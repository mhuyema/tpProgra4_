using Booking.API.Auth;
using Booking.API.Controllers;
using Booking.API.DTOs;
using Booking.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Booking.Tests;

// Pruebas de la regla "cada usuario ve solo SUS reservas" del endpoint de estado.
// El filtro de autenticación ya validó el token y dejó el id del usuario en HttpContext.Items;
// acá se simula ese paso poniéndolo a mano y se prueba qué hace el controller con él.
public class ReservasControllerTests
{
    private static ReservasController NuevoController(Reserva? reserva, string? usuarioAutenticado)
    {
        var http = new DefaultHttpContext();
        http.Items[AutenticacionRestFilter.SubjectItemKey] = usuarioAutenticado;

        return new ReservasController(new FakeReservaService(reserva))
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }

    private static Reserva ReservaDe(string usuarioId, Guid solicitudId) => new()
    {
        Id = Guid.NewGuid(),
        SolicitudId = solicitudId,
        UsuarioId = usuarioId,
        Estado = EstadoReserva.Retenida,
        VenceEn = DateTime.UtcNow.AddMinutes(5)
    };

    [Fact]
    public async Task ObtenerEstado_ReservaPropia_DevuelveElEstado()
    {
        var solicitud = Guid.NewGuid();
        var controller = NuevoController(ReservaDe("ana", solicitud), usuarioAutenticado: "ana");

        var resultado = await controller.ObtenerEstadoSolicitud(solicitud);

        var ok = Assert.IsType<OkObjectResult>(resultado.Result);
        var cuerpo = Assert.IsType<EstadoSolicitudResponse>(ok.Value);
        Assert.Equal(solicitud, cuerpo.RequestId);
        Assert.Equal("Retenida", cuerpo.Status);
    }

    [Fact]
    public async Task ObtenerEstado_ReservaDeOtroUsuario_Devuelve404()
    {
        // Beto conoce el RequestId de Ana: no debe poder ver su reserva ni saber que existe.
        var solicitud = Guid.NewGuid();
        var controller = NuevoController(ReservaDe("ana", solicitud), usuarioAutenticado: "beto");

        var resultado = await controller.ObtenerEstadoSolicitud(solicitud);

        Assert.IsType<NotFoundResult>(resultado.Result);
    }

    [Fact]
    public async Task ObtenerEstado_ReservaInexistente_Devuelve404()
    {
        var controller = NuevoController(reserva: null, usuarioAutenticado: "ana");

        var resultado = await controller.ObtenerEstadoSolicitud(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(resultado.Result);
    }
}
