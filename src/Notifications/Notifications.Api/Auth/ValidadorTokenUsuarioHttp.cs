using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace Notifications.Api.Auth;

// Implementación de IValidadorTokenUsuario: le pregunta al Authorization Server por HTTP.
//   GET {Auth:Authority}/api/auth/validate  con  "Authorization: Bearer <token>"
//   -> 200 { valid, subject, email }   si el token sirve
//   -> 401                             si no sirve
//
// Es casi igual a ValidadorTokenHttp de Booking: mismo servidor, mismo endpoint, misma respuesta.
public class ValidadorTokenUsuarioHttp(HttpClient http, IMemoryCache cache) : IValidadorTokenUsuario
{
    // Una pantalla de seguimiento puede pedir los avisos varias veces seguidas (el botón "Actualizar").
    // Para no molestar al Authorization Server en cada pedido, recordamos un minuto el resultado BUENO.
    private static readonly TimeSpan CacheDuracion = TimeSpan.FromMinutes(1);

    public async Task<ResultadoTokenUsuario> ValidarAsync(string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return ResultadoTokenUsuario.Invalido;

        // El token es un SECRETO (con él cualquiera se hace pasar por el usuario). Por eso nunca lo
        // escribimos en un log ni lo usamos tal cual como clave del caché: usamos su hash SHA-256.
        var clave = "tok:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        if (cache.TryGetValue(clave, out ResultadoTokenUsuario? enCache) && enCache is not null)
            return enCache;

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/validate");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await http.SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return ResultadoTokenUsuario.Invalido;

        // Cualquier otro error (500, 404 por una URL mal configurada) NO dice nada sobre el token:
        // EnsureSuccessStatusCode lanza HttpRequestException y el endpoint responde 503.
        response.EnsureSuccessStatusCode();

        var cuerpo = await response.Content.ReadFromJsonAsync<RespuestaValidacion>(cancellationToken);
        if (cuerpo is not { Valid: true } || string.IsNullOrWhiteSpace(cuerpo.Subject))
            return ResultadoTokenUsuario.Invalido;

        var resultado = new ResultadoTokenUsuario(true, cuerpo.Subject);
        cache.Set(clave, resultado, CacheDuracion);
        return resultado;
    }

    // Forma de la respuesta de /api/auth/validate. Es un "record" privado: solo sirve para leer el JSON.
    private record RespuestaValidacion(bool Valid, string? Subject, string? Email);
}