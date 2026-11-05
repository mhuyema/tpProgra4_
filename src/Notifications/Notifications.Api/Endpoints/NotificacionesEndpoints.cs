using Notifications.Api.Auth;
using Notifications.Api.Services;

namespace Notifications.Api.Endpoints;

// Acá vive el endpoint de consulta. Lo sacamos de Program.cs para que Program.cs quede corto y
// para poder probar la lógica en un test sin levantar todo el servidor.
//
// ¿Qué es un "Minimal API"? En vez de una clase Controller, se declara la ruta con MapGet y una función.
// Este servicio es chico (un solo GET), por eso no hace falta armar un controller completo.
public static class NotificacionesEndpoints
{
    // "this IEndpointRouteBuilder app" lo convierte en un método de extensión:
    // en Program.cs se usa como app.MapNotificacionesEndpoints(), igual que app.MapGet(...).
    public static IEndpointRouteBuilder MapNotificacionesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/notificaciones/{userId}", ObtenerPorUsuarioAsync);
        return app;
    }

    // Es "public static" (y no private) para poder llamarla directo desde los tests.
    // ASP.NET le inyecta solos los parámetros: userId viene de la ruta, y los servicios del contenedor de DI.
    public static async Task<IResult> ObtenerPorUsuarioAsync(
        string userId,
        HttpRequest request,
        IValidadorTokenUsuario validador,
        INotificationStore store,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Notifications.Endpoints");

        // Paso 1: sacar el token del encabezado "Authorization: Bearer xxxxx".
        var token = ExtraerBearer(request.Headers.Authorization.ToString());

        ResultadoTokenUsuario resultado;
        try
        {
            // Paso 2: preguntarle al Authorization Server si el token es válido.
            resultado = await validador.ValidarAsync(token, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            // No pudimos hablar con el Authorization Server. Responder 401 sería injusto (el token puede
            // ser bueno), así que decimos 503 "servicio no disponible" y el cliente puede reintentar.
            logger.LogError(ex, "[AUDITORIA] No se pudo consultar al Authorization Server para validar el token.");
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        // 401 = "no sé quién sos" (sin token, token inválido o vencido).
        if (!resultado.Valido)
        {
            logger.LogWarning("[AUDITORIA] Consulta de avisos rechazada: token ausente, inválido o vencido.");
            return Results.StatusCode(StatusCodes.Status401Unauthorized);
        }

        // Paso 3: 403 = "sé quién sos, pero ese pedido no es tuyo".
        // Un usuario solo puede leer SUS avisos: el subject del token tiene que ser el userId de la URL.
        if (!string.Equals(resultado.Subject, userId, StringComparison.Ordinal))
        {
            logger.LogWarning("[AUDITORIA] Consulta de avisos rechazada: el usuario {Subject} pidió los avisos de otro usuario.",
                resultado.Subject);
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        return Results.Ok(store.GetByUser(userId));
    }

    // El encabezado llega como "Bearer abc123". Nos quedamos solo con "abc123".
    // Devuelve null si el encabezado no existe o no empieza con "Bearer ".
    private static string? ExtraerBearer(string? header)
    {
        const string prefijo = "Bearer ";
        return header is not null && header.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase)
            ? header[prefijo.Length..].Trim()
            : null;
    }
}
