using System.Collections.Concurrent;
using Broker.Messaging.Abstractions;
using Broker.Messaging.Contracts;

namespace Payment.Tests;

internal sealed class TestMessagePublisher : IMessagePublisher
{
    public ConcurrentQueue<AuditLogMessage> Messages { get; } = new();
    public bool FailPublishing { get; init; }

    public Task PublishAsync<T>(T message, string correlationId, CancellationToken cancellationToken = default)
    {
        if (FailPublishing)
            return Task.FromException(new InvalidOperationException("Broker no disponible."));

        if (message is AuditLogMessage audit)
            Messages.Enqueue(audit);
        return Task.CompletedTask;
    }
}
