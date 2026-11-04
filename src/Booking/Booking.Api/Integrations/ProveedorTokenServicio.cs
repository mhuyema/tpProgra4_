using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;

namespace Booking.API.Integrations;

// Obtiene del Authorization Server el token de SERVICIO de Booking (client credentials) para llamar a Payment.
// Booking no fabrica el token: lo pide a /connect/token con su client_id/client_secret y lo reutiliza hasta poco antes de que venza.
public interface IProveedorTokenServicio
{
    Task<string> ObtenerAsync(CancellationToken cancellationToken = default);

    /// <summary>Descarta el token cacheado (p. ej. si Payment lo rechazó) para pedir uno nuevo.</summary>
    void Invalidar();
}

public class ProveedorTokenServicio : IProveedorTokenServicio
{
    private const string CacheKey = "token-servicio-payment";
    private static readonly TimeSpan MargenVencimiento = TimeSpan.FromSeconds(60);

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;
    private readonly SemaphoreSlim _candado = new(1, 1);

    public ProveedorTokenServicio(HttpClient http, IMemoryCache cache, IConfiguration config)
    {
        _http = http;
        _cache = cache;
        _config = config;
    }

    public async Task<string> ObtenerAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out string? token) && token is not null)
            return token;

        await _candado.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(CacheKey, out token) && token is not null)
                return token;

            var clientId = _config["Auth:ClientId"]
                ?? throw new InvalidOperationException("No se configuró Auth:ClientId (cliente de servicio de Booking).");
            var secret = _config["Auth:ClientSecret"]
                ?? throw new InvalidOperationException("No se configuró Auth:ClientSecret (secret del cliente de servicio de Booking).");

            using var request = new HttpRequestMessage(HttpMethod.Post, "connect/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = clientId,
                    ["client_secret"] = secret
                })
            };

            using var response = await _http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var cuerpo = await response.Content.ReadFromJsonAsync<RespuestaToken>(cancellationToken)
                ?? throw new InvalidOperationException("El Authorization Server devolvió una respuesta vacía.");
            if (string.IsNullOrEmpty(cuerpo.AccessToken))
                throw new InvalidOperationException("El Authorization Server no devolvió access_token.");

            var vida = TimeSpan.FromSeconds(cuerpo.ExpiresIn > 0 ? cuerpo.ExpiresIn : 60);
            var cacheado = vida > MargenVencimiento ? vida - MargenVencimiento : vida / 2;
            _cache.Set(CacheKey, cuerpo.AccessToken, cacheado);
            return cuerpo.AccessToken;
        }
        finally
        {
            _candado.Release();
        }
    }

    public void Invalidar() => _cache.Remove(CacheKey);

    private record RespuestaToken(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
