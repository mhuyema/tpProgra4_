using Booking.API.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Booking.Tests;

// Pruebas del filtro que protege los GET de Booking (punto 3.e): quién puede entrar y con qué respuesta se lo rechaza.
// Convención de nombres: Situacion_ResultadoEsperado. Cada prueba sigue el patrón AAA: Arrange / Act / Assert.
public class AutenticacionRestFilterTests
{
    // Arma el "contexto" que ASP.NET le pasaría al filtro, con o sin encabezado Authorization.
    private static AuthorizationFilterContext Contexto(string? token)
    {
        var http = new DefaultHttpContext();
        if (token is not null)
            http.Request.Headers.Authorization = "Bearer " + token;

        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    private static AutenticacionRestFilter Filtro(TipoLlamador tipo, FakeValidadorToken validador, string? servicioPermitido = null)
    {
        var valores = new Dictionary<string, string?>();
        if (servicioPermitido is not null)
            valores["Auth:AllowedServiceClientId"] = servicioPermitido;

        var config = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        return new AutenticacionRestFilter(tipo, validador, config, NullLogger<AutenticacionRestFilter>.Instance);
    }

    private static int Estado(AuthorizationFilterContext contexto) =>
        Assert.IsType<StatusCodeResult>(contexto.Result).StatusCode;

    // ---------- Llamador: Usuario ----------

    [Fact]
    public async Task Usuario_SinToken_Responde401()
    {
        var contexto = Contexto(token: null);

        await Filtro(TipoLlamador.Usuario, new FakeValidadorToken { TokenValido = "abc", Subject = "ana" })
            .OnAuthorizationAsync(contexto);

        Assert.Equal(401, Estado(contexto));
    }

    [Fact]
    public async Task Usuario_TokenInvalido_Responde401()
    {
        var contexto = Contexto("otro-token");

        await Filtro(TipoLlamador.Usuario, new FakeValidadorToken { TokenValido = "abc", Subject = "ana" })
            .OnAuthorizationAsync(contexto);

        Assert.Equal(401, Estado(contexto));
    }

    [Fact]
    public async Task Usuario_AuthorizationServerCaido_Responde503()
    {
        var contexto = Contexto("abc");

        await Filtro(TipoLlamador.Usuario, new FakeValidadorToken { Caido = true })
            .OnAuthorizationAsync(contexto);

        // 503 y no 401: no sabemos si el token es bueno, solo que no pudimos comprobarlo.
        Assert.Equal(503, Estado(contexto));
    }

    [Fact]
    public async Task Usuario_TokenValido_DejaPasarYGuardaElSubject()
    {
        var contexto = Contexto("abc");

        await Filtro(TipoLlamador.Usuario, new FakeValidadorToken { TokenValido = "abc", Subject = "ana" })
            .OnAuthorizationAsync(contexto);

        // Result == null significa "el filtro no cortó el pedido": el endpoint se ejecuta.
        Assert.Null(contexto.Result);
        Assert.Equal("ana", contexto.HttpContext.Items[AutenticacionRestFilter.SubjectItemKey]);
    }

    // ---------- Llamador: ServicioAvailability ----------

    [Fact]
    public async Task Servicio_SinToken_Responde401()
    {
        var contexto = Contexto(token: null);

        await Filtro(TipoLlamador.ServicioAvailability, new FakeValidadorToken { TokenValido = "abc", Subject = "availability-service" })
            .OnAuthorizationAsync(contexto);

        Assert.Equal(401, Estado(contexto));
    }

    [Fact]
    public async Task Servicio_TokenDeUnUsuario_Responde403()
    {
        // Token válido, pero emitido a una persona y no al servicio Availability: no tiene permiso.
        var contexto = Contexto("abc");

        await Filtro(TipoLlamador.ServicioAvailability, new FakeValidadorToken { TokenValido = "abc", Subject = "ana" })
            .OnAuthorizationAsync(contexto);

        Assert.Equal(403, Estado(contexto));
    }

    [Fact]
    public async Task Servicio_TokenDeOtroServicio_Responde403()
    {
        // booking-service tampoco puede consultar este endpoint: solo availability-service.
        var contexto = Contexto("abc");

        await Filtro(TipoLlamador.ServicioAvailability, new FakeValidadorToken { TokenValido = "abc", Subject = "booking-service" })
            .OnAuthorizationAsync(contexto);

        Assert.Equal(403, Estado(contexto));
    }

    [Fact]
    public async Task Servicio_TokenDeAvailability_DejaPasar()
    {
        var contexto = Contexto("abc");

        await Filtro(TipoLlamador.ServicioAvailability, new FakeValidadorToken { TokenValido = "abc", Subject = "availability-service" })
            .OnAuthorizationAsync(contexto);

        Assert.Null(contexto.Result);
    }

    [Fact]
    public async Task Servicio_ElClienteEsperadoSePuedeCambiarPorConfiguracion()
    {
        var contexto = Contexto("abc");

        await Filtro(TipoLlamador.ServicioAvailability,
                new FakeValidadorToken { TokenValido = "abc", Subject = "otro-nombre" },
                servicioPermitido: "otro-nombre")
            .OnAuthorizationAsync(contexto);

        Assert.Null(contexto.Result);
    }
}