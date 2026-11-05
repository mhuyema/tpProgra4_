using System.Net.Http.Headers;
using Availability.Infrastructure.Servicios;
using Microsoft.AspNetCore.Authentication;

namespace Availability.Web;

/// <summary>
/// Agrega el access token del usuario logueado (guardado en la cookie por SaveTokens) como Bearer
/// en las requests a Booking. Si no hay sesión (pantalla de asientos anónima), la request sale sin token.
/// </summary>
public class TokenUsuarioHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Los pedidos etiquetados como "de servicio" (asientos ocupados, pantalla pública) llevan el token de
        // SERVICIO que agrega TokenServicioHandler. Acá no se les pone el del usuario, aunque haya sesión iniciada:
        // Booking rechazaría ese endpoint con un token de usuario (403).
        if (request.Options.TryGetValue(OpcionesDePeticion.UsarTokenDeServicio, out var deServicio) && deServicio)
            return await base.SendAsync(request, cancellationToken);

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            var token = await httpContext.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
