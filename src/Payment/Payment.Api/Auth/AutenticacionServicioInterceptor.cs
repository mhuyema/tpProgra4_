using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Payment.Api.Auth;

// Autentica cada llamada gRPC: exige "authorization: Bearer <token de servicio>" y lo valida contra el
// Authorization Server. Además de ser válido, el token debe haberse emitido al cliente esperado (Booking).
//   Sin token / inválido / vencido -> Unauthenticated (Booking pide un token nuevo y reintenta una vez)
//   Token válido de otro cliente -> PermissionDenied
//   Authorization Server caído -> Unavailable (Booking lo trata como falla transitoria de comunicación)
public class AutenticacionServicioInterceptor(
    IValidadorTokenServicio validador,
    IConfiguration configuration,
    ILogger<AutenticacionServicioInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var clienteEsperado = configuration["Auth:AllowedClientId"] ?? "booking-service";
        var token = ExtraerBearer(context.RequestHeaders.GetValue("authorization"));

        ResultadoTokenServicio resultado;
        try
        {
            resultado = await validador.ValidarAsync(token, context.CancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "No se pudo consultar al Authorization Server para validar el token de servicio.");
            throw new RpcException(new Status(StatusCode.Unavailable, "No se pudo validar la identidad del llamador."));
        }

        if (!resultado.Valido)
        {
            logger.LogWarning("Llamada a Payment rechazada: token de servicio ausente, inválido o vencido. TokenPresente={TokenPresente}",
                token is not null);
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Token ausente, inválido o vencido."));
        }

        if (!string.Equals(resultado.Subject, clienteEsperado, StringComparison.Ordinal))
        {
            logger.LogWarning("Llamada a Payment rechazada: el cliente {Cliente} no tiene permiso para cobrar.", resultado.Subject);
            throw new RpcException(new Status(StatusCode.PermissionDenied, "El llamador no tiene permiso para cobrar."));
        }

        return await continuation(request, context);
    }

    private static string? ExtraerBearer(string? header)
    {
        const string prefijo = "Bearer ";
        return header is not null && header.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase)
            ? header[prefijo.Length..].Trim()
            : null;
    }
}
