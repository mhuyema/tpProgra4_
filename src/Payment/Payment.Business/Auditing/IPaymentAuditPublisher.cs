namespace Payment.Business.Auditing;

/// <summary>Publica eventos de auditoría de Payment usando el broker configurado.</summary>
public interface IPaymentAuditPublisher
{
    Task InitProcessAsync(string correlationId, string processName,
        Dictionary<string, object?>? details = null, CancellationToken cancellationToken = default);

    Task EndProcessAsync(string correlationId, string processName, object? result,
        Dictionary<string, object?>? details = null, CancellationToken cancellationToken = default);

    Task EmitErrorAsync(string correlationId, string processName, Exception exception,
        Dictionary<string, object?>? details = null, CancellationToken cancellationToken = default);
}
