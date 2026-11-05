using Broker.Messaging.Abstractions;
using Broker.Messaging.Contracts;

namespace Notifications.Api.Auditing;

// Implementación real: arma un AuditLogMessage (el mensaje de auditoría COMPARTIDO por todos los
// servicios, vive en Broker.Messaging) y lo publica en el topic. El servicio AuditLog lo recibe y lo guarda.
//
// Es casi igual a PaymentAuditPublisher a propósito: si todos los servicios auditan con la misma
// forma, el servicio AuditLog los puede mostrar todos juntos y ordenados por CorrelationId.

public sealed class NotificationAuditPublisher(
    IMessagePublisher publisher,
    ILogger<NotificationAuditPublisher> logger) : INotificationAuditPublisher
{
    // Nombre con el que este servicio aparece en el AuditLog.
    private const string ServiceName = "Notifications";

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

    // Copia los detalles que nos pasan para no modificar el diccionario original de quien nos llamó,
    // y agrega el nombre del proceso.
    private static Dictionary<string, object?> CreateDetails(string processName,
        Dictionary<string, object?>? details)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processName);

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
            Service: ServiceName,
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
            // Auditar nunca debe romper lo que se está auditando.
            // Si el broker falla acá, solo lo dejamos anotado en el log de la consola y seguimos:
            // el aviso al usuario es más importante que el registro de auditoría.
            logger.LogError(exception,
                "No se pudo publicar la auditoría {Action}. CorrelationId={CorrelationId}",
                action, correlationId);
        }
    }
}