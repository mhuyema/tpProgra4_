namespace Payment.Api.Auth;

// Verifica el token de servicio con el que otro microservicio (Booking) llama a Payment.
// La implementación delega en el Authorization Server (GET /api/auth/validate).
public interface IValidadorTokenServicio
{
    /// <summary>
    /// Devuelve <see cref="ResultadoTokenServicio.Invalido"/> si el token falta, es inválido o venció.
    /// Lanza <see cref="HttpRequestException"/> si no se pudo consultar al Authorization Server.
    /// </summary>
    Task<ResultadoTokenServicio> ValidarAsync(string? token, CancellationToken cancellationToken = default);
}

/// <param name="Valido">True si el Authorization Server aceptó el token.</param>
/// <param name="Subject">Cliente al que se emitió el token (claim sub = client_id).</param>
public record ResultadoTokenServicio(bool Valido, string? Subject)
{
    public static readonly ResultadoTokenServicio Invalido = new(false, null);
}
