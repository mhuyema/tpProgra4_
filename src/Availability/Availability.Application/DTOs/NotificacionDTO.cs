namespace Availability.Application.DTOs
{
    /// <summary>
    /// Un aviso que el servicio de Notificaciones le mandó al usuario ("tu reserva fue creada",
    /// "tu reserva fue confirmada"). Es un DTO (Data Transfer Object): un objeto que solo lleva datos
    /// de una capa a otra, sin lógica. Availability lo recibe de Notifications y lo muestra en pantalla.
    /// </summary>
    /// <param name="SolicitudId">Solicitud de reserva a la que pertenece el aviso (sirve para ubicarlo).</param>
    /// <param name="Tipo">"ReservaCreada" o "ReservaConfirmada".</param>
    /// <param name="Mensaje">Texto listo para mostrarle al usuario.</param>
    /// <param name="EnviadaUtc">Momento en que se envió el aviso (en UTC).</param>
    public record NotificacionDTO(
        Guid SolicitudId,
        string Tipo,
        string Mensaje,
        DateTime EnviadaUtc);
}
