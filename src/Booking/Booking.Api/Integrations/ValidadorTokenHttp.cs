using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Booking.Business.Auth;
using Microsoft.Extensions.Caching.Memory;

namespace Booking.API.Integrations;

// Adaptador que implementa IValidadorToken llamando al Authorization Server:
// GET /api/auth/validate con "Authorization: Bearer <token>" -> 200 { valid, subject, email } | 401.
// El token es un secreto: nunca se loguea ni se usa como clave de caché en claro.
public class ValidadorTokenHttp : IValidadorToken
{
    // Una reserva de hasta 6 asientos publica 6 mensajes con el mismo token: se cachea el resultado válido un instante.
    private static readonly TimeSpan CacheDuracion = TimeSpan.FromMinutes(1);

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ValidadorTokenHttp> _logger;

    public ValidadorTokenHttp(HttpClient http, IMemoryCache cache, ILogger<ValidadorTokenHttp> logger)
    {
        _http = http;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ResultadoToken> ValidarAsync(string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return ResultadoToken.Invalido;

        var clave = "tok:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        if (_cache.TryGetValue(clave, out ResultadoToken? enCache) && enCache is not null)
            return enCache;

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/validate");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Si el Authorization Server no responde, HttpRequestException sube: el broker reintenta el mensaje.
        using var response = await _http.SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return ResultadoToken.Invalido;

        // Cualquier otro fallo (5xx, 404 por mala configuración) no dice nada del token.
        response.EnsureSuccessStatusCode();

        var cuerpo = await response.Content.ReadFromJsonAsync<RespuestaValidacion>(cancellationToken);
        var resultado = cuerpo is { Valid: true } && !string.IsNullOrWhiteSpace(cuerpo.Subject)
            ? new ResultadoToken(true, cuerpo.Subject)
            : ResultadoToken.Invalido;

        if (resultado.Valido)
            _cache.Set(clave, resultado, CacheDuracion);

        _logger.LogInformation("[AUDITORIA] Token validado contra el Authorization Server. Valido={Valido}", resultado.Valido);
        return resultado;
    }

    // Contrato de GET /api/auth/validate del Authorization Server (propiedades en camelCase).
    private record RespuestaValidacion(bool Valid, string? Subject, string? Email);
}
