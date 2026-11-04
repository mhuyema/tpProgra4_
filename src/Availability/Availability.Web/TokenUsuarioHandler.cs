using System.Net.Http.Headers;
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
