using Payment.Api.Auth;

namespace Payment.Tests;

// Reemplaza la consulta HTTP al Authorization Server en los tests.
public sealed class FakeValidadorTokenServicio : IValidadorTokenServicio
{
    public const string TokenBooking = "token-booking";
    public const string TokenOtroServicio = "token-otro-servicio";

    public Task<ResultadoTokenServicio> ValidarAsync(string? token, CancellationToken cancellationToken = default) =>
        Task.FromResult(token switch
        {
            TokenBooking => new ResultadoTokenServicio(true, "booking-service"),
            TokenOtroServicio => new ResultadoTokenServicio(true, "otro-servicio"),
            _ => ResultadoTokenServicio.Invalido
        });
}

// Agrega "authorization: Bearer <token>" a cada llamada del canal gRPC de prueba.
public sealed class EncabezadoBearerHandler(string? token, HttpMessageHandler inner) : DelegatingHandler(inner)
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (token is not null)
            request.Headers.TryAddWithoutValidation("authorization", $"Bearer {token}");
        return base.SendAsync(request, cancellationToken);
    }
}
