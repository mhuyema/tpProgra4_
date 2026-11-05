using System.Text.Json.Serialization;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace Availability.Infrastructure.Servicios
{
    /// <summary>
    /// Entrega el token de servicio de Availability (client credentials) para llamar a Booking.
    /// Availability no fabrica el token: se lo pide al Authorization Server (POST /connect/token) con su
    /// client_id y client_secret, y lo reutiliza hasta poco antes de que venza para no pedir uno nuevo en cada llamada.
    /// Es el mismo patrón que ProveedorTokenServicio de Booking (que lo usa para llamar a Payment).
    /// </summary>
    public interface IProveedorTokenServicio
    {
        Task<string> ObtenerAsync(CancellationToken cancellationToken = default);

        /// <summary>Descarta el token guardado (por ejemplo si Booking lo rechazó) para pedir uno nuevo.</summary>
        void Invalidar();
    }

    public class ProveedorTokenServicio : IProveedorTokenServicio
    {
        // Nombre del HttpClient (configurado en DependencyInjection) que apunta al Authorization Server.
        public const string NombreCliente = "AuthorizationServer";

        // Se pide un token nuevo un minuto antes de que venza, para no usar uno que vence a mitad de viaje.
        private static readonly TimeSpan MargenVencimiento = TimeSpan.FromSeconds(60);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        // Evita que dos pedidos simultáneos pidan dos tokens a la vez: el segundo espera y reutiliza el del primero.
        private readonly SemaphoreSlim _candado = new(1, 1);

        // Un solo objeto inmutable con el token y su vencimiento. Se lee y se reemplaza entero, así nunca
        // se puede leer un token de una vez con el vencimiento de otra. "volatile" asegura que todos los hilos vean el último valor.
        private volatile TokenGuardado? _guardado;

        public ProveedorTokenServicio(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public async Task<string> ObtenerAsync(CancellationToken cancellationToken = default)
        {
            if (TokenVigente(out var vigente))
                return vigente;

            await _candado.WaitAsync(cancellationToken);
            try
            {
                // Se vuelve a mirar: mientras esperábamos el candado, otro pedido pudo haber obtenido el token.
                if (TokenVigente(out vigente))
                    return vigente;

                var clientId = _config["Auth:ServiceClientId"] ?? "availability-service";
                var secret = _config["Auth:ServiceClientSecret"]
                    ?? throw new InvalidOperationException(
                        "No se configuró Auth:ServiceClientSecret (secret del cliente de servicio de Availability).");

                using var request = new HttpRequestMessage(HttpMethod.Post, "connect/token")
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["grant_type"] = "client_credentials",
                        ["client_id"] = clientId,
                        ["client_secret"] = secret
                    })
                };

                var cliente = _httpClientFactory.CreateClient(NombreCliente);
                using var response = await cliente.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var cuerpo = await response.Content.ReadFromJsonAsync<RespuestaToken>(cancellationToken)
                    ?? throw new InvalidOperationException("El Authorization Server devolvió una respuesta vacía.");
                if (string.IsNullOrEmpty(cuerpo.AccessToken))
                    throw new InvalidOperationException("El Authorization Server no devolvió access_token.");

                var vida = TimeSpan.FromSeconds(cuerpo.ExpiresIn > 0 ? cuerpo.ExpiresIn : 60);
                var util = vida > MargenVencimiento ? vida - MargenVencimiento : vida / 2;
                _guardado = new TokenGuardado(cuerpo.AccessToken, DateTime.UtcNow + util);
                return cuerpo.AccessToken;
            }
            finally
            {
                _candado.Release();
            }
        }

        public void Invalidar() => _guardado = null;

        private bool TokenVigente(out string token)
        {
            var actual = _guardado;
            if (actual is not null && DateTime.UtcNow < actual.VenceUtc)
            {
                token = actual.Valor;
                return true;
            }

            token = string.Empty;
            return false;
        }

        private sealed record TokenGuardado(string Valor, DateTime VenceUtc);

        // Forma de la respuesta de /connect/token. Solo sirve para leer el JSON.
        private record RespuestaToken(
            [property: JsonPropertyName("access_token")] string? AccessToken,
            [property: JsonPropertyName("expires_in")] int ExpiresIn);
    }
}