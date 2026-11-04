namespace Booking.Business.Auth;

// Contrato interno de Booking para verificar la identidad del usuario que solicita una reserva.
// La implementación delega en el Authorization Server (GET /api/auth/validate), así Business
// no conoce HTTP ni OpenIddict (mismo criterio que IPagoGateway).
public interface IValidadorToken
{
    /// <summary>
    /// Valida el access token. Devuelve <see cref="ResultadoToken.Invalido"/> si falta, es inválido o venció.
    /// Lanza excepción si no se pudo consultar al Authorization Server (no es culpa del token: se reintenta).
    /// </summary>
    Task<ResultadoToken> ValidarAsync(string? token, CancellationToken cancellationToken = default);
}

/// <param name="Valido">True si el Authorization Server aceptó el token.</param>
/// <param name="Subject">Identificador del usuario (claim sub) al que pertenece el token.</param>
public record ResultadoToken(bool Valido, string? Subject)
{
    public static readonly ResultadoToken Invalido = new(false, null);
}
