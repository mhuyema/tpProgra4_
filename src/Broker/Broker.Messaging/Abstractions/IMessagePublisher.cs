namespace Broker.Messaging.Abstractions;

/// <summary>
/// Puerto de salida para publicar mensajes (lo usa Availability).
/// Los servicios dependen de esta interfaz y no de Azure Service Bus: así el broker se puede
/// reemplazar (o falsear en tests) sin tocar la lógica de negocio.
/// </summary>
public interface IMessagePublisher
{
    /// <param name="message">Cuerpo del mensaje, se serializa a JSON.</param>
    /// <param name="correlationId">Id que identifica la cadena de sucesos completa (en una reserva, el requestId).</param>
    Task PublishAsync<T>(T message, string correlationId, CancellationToken cancellationToken = default);
}
