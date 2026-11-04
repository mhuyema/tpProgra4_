using Availability.Application.Interfaces;

namespace Availability.Infrastructure.Broker
{
    /// <summary>
    /// No-op: se usa cuando no hay cadena de conexión al broker (mismo criterio que Publicador).
    /// </summary>
    public sealed class AuditoriaPublicadorNulo : IAuditoriaPublicador
    {
        public Task IniciarProcesoAsync(string correlationId, string proceso,
            Dictionary<string, object?>? detalles = null, CancellationToken ct = default) => Task.CompletedTask;

        public Task FinalizarProcesoAsync(string correlationId, string proceso, object? resultado,
            Dictionary<string, object?>? detalles = null, CancellationToken ct = default) => Task.CompletedTask;

        public Task EmitirErrorAsync(string correlationId, string proceso, Exception excepcion,
            Dictionary<string, object?>? detalles = null, CancellationToken ct = default) => Task.CompletedTask;
    }
}
