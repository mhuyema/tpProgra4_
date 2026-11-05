namespace Notifications.Api.Auth;

// ---------------------------------------------------------------------------------------------
// ¿Qué problema resuelve esto?
// El endpoint GET /api/notificaciones/{userId} devuelve avisos privados de una persona. Antes
// cualquiera podía pedir los de cualquier userId. Ahora hay que mandar el token de sesión
// ("Authorization: Bearer <token>") y Notifications le pregunta al Authorization Server si es válido.
// Así cumplimos el punto 3.e del TP: los componentes se autentican con el servidor de autorización.
//
// Es el mismo patrón que IValidadorToken en Booking: una interfaz chica + una implementación
// que llama por HTTP. Si algún día cambia cómo se valida, solo se cambia la implementación.
// ---------------------------------------------------------------------------------------------
public interface IValidadorTokenUsuario
{
    /// <summary>
    /// Devuelve <see cref="ResultadoTokenUsuario.Invalido"/> si el token falta, es inválido o venció.
    /// Lanza <see cref="HttpRequestException"/> si no se pudo consultar al Authorization Server
    /// (en ese caso NO sabemos si el token es bueno o malo, por eso no se confunde con "inválido").
    /// </summary>
    Task<ResultadoTokenUsuario> ValidarAsync(string? token, CancellationToken cancellationToken = default);
}

/// <param name="Valido">True si el Authorization Server aceptó el token.</param>
/// <param name="Subject">Id del usuario dueño del token (claim "sub"). Coincide con el UserId de los avisos.</param>
public record ResultadoTokenUsuario(bool Valido, string? Subject)
{
    // Un valor ya armado para el caso "token malo", así no se crea uno nuevo cada vez.
    public static readonly ResultadoTokenUsuario Invalido = new(false, null);
}
