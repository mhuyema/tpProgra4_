using Availability.Application.Interfaces;
using Availability.Domain.Entities;


namespace Availability.Application.CasosDeUso
{
    /// <summary>
    /// Caso de uso: pedir una reserva. Registra una solicitud con id único por asiento,
    /// las publica en el broker y responde sin esperar el resultado del procesamiento.
    /// </summary>
    public class SolicitarReserva (IAsientoRepositorio asientos, ISolicitudReservaRepositorio solicitudes, IPublicadorSolicitudes publicador, IAuditoriaPublicador auditoria)
    {
        public const int MaxAsientos = 6;

        public async Task<List<Guid>> EjecutarAsync (int IdEvento, IReadOnlyList<int> IdAsientos, string IdUsuario, string nombre, string email, string accessToken)
        {
            // Id del pedido completo (hasta 6 asientos). Cada solicitud individual se audita además con su RequestId,
            // el mismo que viaja al broker, para seguir la cadena Availability -> Booking.
            var pedidoId = Guid.NewGuid().ToString();
            var detallesPedido = new Dictionary<string, object?>
            {
                ["EventoId"] = IdEvento, ["AsientoIds"] = IdAsientos, ["UsuarioId"] = IdUsuario
            };
            await auditoria.IniciarProcesoAsync(pedidoId, "SolicitarReserva", detallesPedido);

            try
            {
            var ids = IdAsientos.Distinct().ToList();

            if (ids.Count == 0)
                throw new Exception("Elegí al menos un asiento.");
            if (ids.Count > MaxAsientos)
                throw new Exception($"Podés reservar hasta {MaxAsientos} asientos.");

            var creadas = new List<SolicitudReserva>();
            foreach (var idAsiento in ids)
            {
                var asiento = await asientos.ObtenerPorIdAsync(idAsiento) ?? throw new Exception("El asiento no existe.");
                if (asiento.EventoId != IdEvento)
                    throw new Exception("El asiento no pertenece al evento.");

                creadas.Add(new SolicitudReserva
                {
                    RequestId = Guid.NewGuid(),
                    AsientoId = idAsiento,
                    UsuarioId = IdUsuario,
                    EventoId = IdEvento,
                    Monto = asiento.Precio,
                });
            }

            foreach (var solicitud in creadas)
            {
                await solicitudes.GuardarAsync(solicitud);
                await publicador.PublicarAsync(solicitud, accessToken);

                // Nunca se incluye el accessToken en los detalles de auditoría.
                await auditoria.FinalizarProcesoAsync(solicitud.RequestId.ToString(), "PublicarSolicitudReserva",
                    new { solicitud.RequestId },
                    new Dictionary<string, object?>
                    {
                        ["PedidoId"] = pedidoId, ["EventoId"] = solicitud.EventoId,
                        ["AsientoId"] = solicitud.AsientoId, ["Monto"] = solicitud.Monto
                    });
            }

            var requestIds = creadas.Select(s => s.RequestId).ToList();
            await auditoria.FinalizarProcesoAsync(pedidoId, "SolicitarReserva", new { RequestIds = requestIds }, detallesPedido);
            return requestIds;
            }
            catch (Exception ex)
            {
                await auditoria.EmitirErrorAsync(pedidoId, "SolicitarReserva", ex, detallesPedido);
                throw;
            }
        }
    }   
}
