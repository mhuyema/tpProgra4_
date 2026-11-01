using AuditLog.Api.Data;
using AuditLog.Api.Models;
using Broker.Messaging.Abstractions;
using Broker.Messaging.Contracts;
using System.Text.Json;

namespace AuditLog.Api.Handlers;

public sealed class AuditLogMessageHandler(AuditLogRepository repository) : IMessageHandler<AuditLogMessage>
{
    public Task HandleAsync(AuditLogMessage message, string correlationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var logId = message.LogId is null || message.LogId == Guid.Empty ? Guid.NewGuid() : message.LogId.Value;
        var date = message.DateUtc is null || message.DateUtc == default(DateTime)
            ? DateTime.UtcNow : message.DateUtc.Value;
        date = date.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(date, DateTimeKind.Utc) : date.ToUniversalTime();

        repository.Save(new AuditLogEntry(logId, date, message.Service, message.Action,
            message.Message, message.CorrelationId,
            message.Details is null ? null : JsonSerializer.SerializeToElement(message.Details)));
        return Task.CompletedTask;
    }
}
