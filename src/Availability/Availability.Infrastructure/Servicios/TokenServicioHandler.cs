using System.Net;
using System.Net.Http.Headers;

namespace Availability.Infrastructure.Servicios
{
    /// <summary>
    /// Un "handler" es un eslabón por el que pasa cada pedido de un HttpClient antes de salir a la red.
    /// Este agrega el token de SERVICIO de Availability, pero SOLO a los pedidos etiquetados con
    /// <see cref="OpcionesDePeticion.UsarTokenDeServicio"/>. Al resto (los que llevan el token del usuario) no los toca.
    ///
    /// DelegatingHandler: después de hacer su trabajo llama a base.SendAsync, que pasa el pedido al siguiente eslabón.
    /// </summary>
    public class TokenServicioHandler(IProveedorTokenServicio proveedor) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var usarServicio = request.Options.TryGetValue(OpcionesDePeticion.UsarTokenDeServicio, out var valor) && valor;
            if (!usarServicio)
                return await base.SendAsync(request, cancellationToken);

            var token = await proveedor.ObtenerAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await base.SendAsync(request, cancellationToken);

            // Si Booking dice que el token no sirve (por ejemplo, el Authorization Server lo revocó), lo descartamos:
            // el próximo pedido va a pedir uno nuevo en lugar de repetir el mismo token malo.
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                proveedor.Invalidar();

            return response;
        }
    }
}