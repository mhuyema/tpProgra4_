using Booking.Business.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Booking.API.Auth;

// Filtro que corre ANTES de cada endpoint marcado con [RequiereToken(...)].
// Lee el "Authorization: Bearer <token>", le pregunta al Authorization Server si el token es válido
// (con el mismo IValidadorToken que Booking ya usa para las solicitudes del broker) y decide:
//
//   Sin token / inválido / vencido ...... 401 (no sé quién sos)
//   Token válido pero de otro llamador .. 403 (sé quién sos, pero no podés entrar acá)
//   Authorization Server no responde .... 503 (no puedo comprobar el token; reintentá más tarde)
//   Todo bien ........................... el endpoint se ejecuta
//
// Es el mismo criterio que el interceptor de Payment (AutenticacionServicioInterceptor) y el endpoint de
// Notifications, así los tres servicios responden igual ante los mismos problemas.
public class AutenticacionRestFilter : IAsyncAuthorizationFilter
{
    // Clave con la que el filtro deja el id del usuario autenticado para que lo lea el controller.
    // HttpContext.Items es un diccionario que vive solo durante UN pedido HTTP.
    public const string SubjectItemKey = "Auth.Subject";

    private readonly TipoLlamador _tipo;
    private readonly IValidadorToken _validador;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AutenticacionRestFilter> _logger;

    // "tipo" lo pasa el atributo; el resto lo inyecta .NET.
    public AutenticacionRestFilter(
        TipoLlamador tipo,
        IValidadorToken validador,
        IConfiguration configuration,
        ILogger<AutenticacionRestFilter> logger)
    {
        _tipo = tipo;
        _validador = validador;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var cancellationToken = context.HttpContext.RequestAborted;
        var token = ExtraerBearer(context.HttpContext.Request.Headers.Authorization.ToString());

        ResultadoToken resultado;
        try
        {
            resultado = await _validador.ValidarAsync(token, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException
                                   || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            // No pudimos hablar con el Authorization Server (caído o demasiado lento). Responder 401 sería
            // injusto, porque el token puede ser bueno: decimos 503 para que el cliente pueda reintentar.
            // (TaskCanceledException sin que el cliente haya cancelado = se agotó el timeout del HttpClient.)
            _logger.LogError(ex, "[AUDITORIA] No se pudo consultar al Authorization Server para validar el token.");
            context.Result = new StatusCodeResult(StatusCodes.Status503ServiceUnavailable);
            return;
        }

        if (!resultado.Valido)
        {
            // Ojo: nunca se escribe el token en el log, es un secreto.
            _logger.LogWarning("[AUDITORIA] Pedido rechazado en {Ruta}: token ausente, inválido o vencido. TokenPresente={TokenPresente}",
                context.HttpContext.Request.Path, token is not null);
            context.Result = new StatusCodeResult(StatusCodes.Status401Unauthorized);
            return;
        }

        if (_tipo == TipoLlamador.ServicioAvailability)
        {
            // El token tiene que haberse emitido al cliente esperado. Si fuera el token de un usuario,
            // su sub sería un id de persona y no coincidiría: ese caso se rechaza con 403.
            var esperado = _configuration["Auth:AllowedServiceClientId"] ?? "availability-service";
            if (!string.Equals(resultado.Subject, esperado, StringComparison.Ordinal))
            {
                _logger.LogWarning("[AUDITORIA] Pedido rechazado en {Ruta}: el llamador no es el servicio {Esperado}.",
                    context.HttpContext.Request.Path, esperado);
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                return;
            }
        }

        // Dejamos el sub a mano para que el controller pueda comprobar que el recurso es de ESTE usuario.
        context.HttpContext.Items[SubjectItemKey] = resultado.Subject;
    }

    // El encabezado llega como "Bearer abc123". Nos quedamos solo con "abc123" (o null si no hay).
    private static string? ExtraerBearer(string? header)
    {
        const string prefijo = "Bearer ";
        return header is not null && header.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase)
            ? header[prefijo.Length..].Trim()
            : null;
    }
}

