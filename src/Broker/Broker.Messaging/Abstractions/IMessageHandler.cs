namespace Broker.Messaging.Abstractions;

/// <summary>
/// Puerto de entrada para consumir mensajes (lo implementa Booking).
/// Si HandleAsync lanza una excepción el mensaje NO se confirma y el broker lo reentrega;
/// por eso el handler debe ser idempotente (un mismo requestId puede llegar más de una vez).
/// </summary>
public interface IMessageHandler<in T>
{
    Task HandleAsync(T message, string correlationId, CancellationToken cancellationToken);
}
