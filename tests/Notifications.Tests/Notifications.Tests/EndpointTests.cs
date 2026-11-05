using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging.Abstractions;
using Notifications.Api.Endpoints;
using Notifications.Api.Services;
using Xunit;

namespace Notifications.Tests;

// Pruebas de seguridad del GET /api/notificaciones/{userId}: sin token no se entra (401), con el token de
// otro usuario tampoco (403), y si el Authorization Server no responde se informa 503.
// No se levanta un servidor: se llama directo al método del endpoint con un HttpContext de mentira.
public class EndpointTests
{
    // DefaultHttpContext es un "pedido HTTP" vacío que ASP.NET trae para pruebas.
    private static HttpRequest PedidoConToken(string? token)
    {
        var contexto = new DefaultHttpContext();
        if (token is not null)
            contexto.Request.Headers.Authorization = "Bearer " + token;
        return contexto.Request;
    }

    private static InMemoryNotificationStore StoreConUnAvisoDeAna()
    {
        var store = new InMemoryNotificationStore();
        store.TryAdd(new Notification(Guid.NewGuid(), "ReservaCreada", "ana", "texto", DateTime.UtcNow));
        return store;
    }

    private static Task<IResult> Llamar(string userId, HttpRequest pedido, FakeValidadorToken validador, InMemoryNotificationStore store) =>
        NotificacionesEndpoints.ObtenerPorUsuarioAsync(
            userId, pedido, validador, store, NullLoggerFactory.Instance, CancellationToken.None);

    [Fact]
    public async Task SinToken_Responde401()
    {
        var validador = new FakeValidadorToken { TokenValido = "abc", Subject = "ana" };

        var resultado = await Llamar("ana", PedidoConToken(null), validador, StoreConUnAvisoDeAna());

        Assert.Equal(401, Assert.IsType<StatusCodeHttpResult>(resultado).StatusCode);
    }

    [Fact]
    public async Task TokenInvalido_Responde401()
    {
        var validador = new FakeValidadorToken { TokenValido = "abc", Subject = "ana" };

        var resultado = await Llamar("ana", PedidoConToken("otro-token"), validador, StoreConUnAvisoDeAna());

        Assert.Equal(401, Assert.IsType<StatusCodeHttpResult>(resultado).StatusCode);
    }

    [Fact]
    public async Task TokenDeOtroUsuario_Responde403()
    {
        // El token es de "beto" pero pide los avisos de "ana": no puede.
        var validador = new FakeValidadorToken { TokenValido = "abc", Subject = "beto" };

        var resultado = await Llamar("ana", PedidoConToken("abc"), validador, StoreConUnAvisoDeAna());

        Assert.Equal(403, Assert.IsType<StatusCodeHttpResult>(resultado).StatusCode);
    }

    [Fact]
    public async Task AuthorizationServerCaido_Responde503()
    {
        var validador = new FakeValidadorToken { Caido = true };

        var resultado = await Llamar("ana", PedidoConToken("abc"), validador, StoreConUnAvisoDeAna());

        Assert.Equal(503, Assert.IsType<StatusCodeHttpResult>(resultado).StatusCode);
    }

    [Fact]
    public async Task TokenDelPropioUsuario_DevuelveSusAvisos()
    {
        var validador = new FakeValidadorToken { TokenValido = "abc", Subject = "ana" };

        var resultado = await Llamar("ana", PedidoConToken("abc"), validador, StoreConUnAvisoDeAna());

        var ok = Assert.IsType<Ok<IReadOnlyList<Notification>>>(resultado);
        Assert.Single(ok.Value!);
    }
}
