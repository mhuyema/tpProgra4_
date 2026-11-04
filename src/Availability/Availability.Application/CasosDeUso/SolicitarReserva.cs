using Availability.Application.Interfaces;
using Availability.Domain.Entities;


namespace Availability.Application.CasosDeUso
{
    /// <summary>
    /// Caso de uso: pedir una reserva. Registra una solicitud con id único por asiento,
    /// las publica en el broker y responde sin esperar el resultado del procesamiento.
    /// </summary>
    public class SolicitarReserva (IAsientoRepositorio asientos, ISolicitudReservaRepositorio solicitudes, IPublicadorSolicitudes publicador)
    {
        public const int MaxAsientos = 6;

        public async Task<List<Guid>> EjecutarAsync (int IdEvento, IReadOnlyList<int> IdAsientos, string IdUsuario, string nombre, string email, string accessToken)
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
            }

            return creadas.Select(s => s.RequestId).ToList();
        }
    }   
}
