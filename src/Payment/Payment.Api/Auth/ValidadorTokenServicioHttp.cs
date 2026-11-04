using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace Payment.Api.Auth;

// Validación delegada: GET /api/auth/validate con "Authorization: Bearer <token>" ->
// 200 { valid, subject, email } | 401. El token es un secreto: no se loguea ni se usa en claro como clave de caché.
public class ValidadorTokenServicioHttp(HttpClient http, IMemoryCache cache) : IValidadorTokenServicio
{
    // El mismo token de servicio se reutiliza en muchas llamadas: se cachea el resultado válido un instante.
    private static readonly TimeSpan CacheDuracion = TimeSpan.FromMinutes(1);

    public async Task<ResultadoTokenServicio> ValidarAsync(string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return ResultadoTokenServicio.Invalido;

        var clave = "tok:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        if (cache.TryGetValue(clave, out ResultadoTokenServicio? enCache) && enCache is not null)
            return enCache;

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/validate");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await http.SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return ResultadoTokenServicio.Invalido;

        // Cualquier otro fallo (5xx, 404 por mala configuración) no dice nada del token.
        response.EnsureSuccessStatusCode();

        var cuerpo = await response.Content.ReadFromJsonAsync<RespuestaValidacion>(cancellationToken);
        if (cuerpo is not { Valid: true } || string.IsNullOrWhiteSpace(cuerpo.Subject))
            return ResultadoTokenServicio.Invalido;

        var resultado = new ResultadoTokenServicio(true, cuerpo.Subject);
        cache.Set(clave, resultado, CacheDuracion);
        return resultado;
    }

    // Contrato de GET /api/auth/validate del Authorization Server (propiedades en camelCase).
    private record RespuestaValidacion(bool Valid, string? Subject, string? Email);
}
