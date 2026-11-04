namespace Availability.Application.Interfaces
{
    /// <summary>
    /// Puerto de auditoría: publica eventos de proceso (inicio, fin, error) para el servicio AuditLog.
    /// La implementación nunca debe interrumpir el flujo que audita.
    /// </summary>
    public interface IAuditoriaPublicador
    {
        Task IniciarProcesoAsync(string correlationId, string proceso,
            Dictionary<string, object?>? detalles = null, CancellationToken ct = default);

        Task FinalizarProcesoAsync(string correlationId, string proceso, object? resultado,
            Dictionary<string, object?>? detalles = null, CancellationToken ct = default);

        Task EmitirErrorAsync(string correlationId, string proceso, Exception excepcion,
            Dictionary<string, object?>? detalles = null, CancellationToken ct = default);
    }
}
