using Broker.Messaging.Abstractions;
using Broker.Messaging.Contracts;
using Microsoft.Extensions.Logging;
using Payment.Business.Auditing;

namespace Payment.Api.Auditing;

public sealed class PaymentAuditPublisher(
    IMessagePublisher publisher, ILogger<PaymentAuditPublisher> logger) : IPaymentAuditPublisher
{
    public Task InitProcessAsync(string correlationId, string processName,
        Dictionary<string, object?>? details = null, CancellationToken cancellationToken = default)
    {
        var eventDetails = CreateDetails(processName, details);
        return PublishAsync(correlationId, "InitProcess", $"Inicio del proceso {processName}.",
            eventDetails, cancellationToken);
    }

    public Task EndProcessAsync(string correlationId, string processName, object? result,
        Dictionary<string, object?>? details = null, CancellationToken cancellationToken = default)
    {
        var eventDetails = CreateDetails(processName, details);
        eventDetails["Result"] = result;
        return PublishAsync(correlationId, "EndProcess", $"Finalización del proceso {processName}.",
            eventDetails, cancellationToken);
    }

    public Task EmitErrorAsync(string correlationId, string processName, Exception exception,
        Dictionary<string, object?>? details = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var eventDetails = CreateDetails(processName, details);
        eventDetails["ErrorType"] = exception.GetType().FullName;
        eventDetails["ErrorMessage"] = exception.Message;
        eventDetails["StackTrace"] = exception.StackTrace;
        return PublishAsync(correlationId, "EmitError", $"Error en el proceso {processName}: {exception.Message}",
            eventDetails, cancellationToken);
    }

    private static Dictionary<string, object?> CreateDetails(string processName,
        Dictionary<string, object?>? details)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processName);

        // Copiar para no modificar los datos del llamador.
        var eventDetails = details is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(details);
        eventDetails["ProcessName"] = processName;
        return eventDetails;
    }

    private async Task PublishAsync(string correlationId, string action, string message,
        Dictionary<string, object?> details, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        var auditLog = new AuditLogMessage(
            LogId: Guid.NewGuid(),
            DateUtc: DateTime.UtcNow,
            Service: "Payment",
            Action: action,
            Message: message,
            CorrelationId: correlationId,
            Details: details);

        try
        {
            await publisher.PublishAsync(auditLog, correlationId, cancellationToken);
        }
        catch (Exception exception)
        {
            // La auditoría no debe impedir el pago ni reemplazar su error original.
            logger.LogError(exception,
                "No se pudo publicar la auditoría {Action}. CorrelationId={CorrelationId}",
                action, correlationId);
        }
    }
}
