using Availability.Application.Interfaces;
using Broker.Messaging.Abstractions;
using Broker.Messaging.Contracts;
using Microsoft.Extensions.Logging;

namespace Availability.Infrastructure.Broker
{
    /// <summary>
    /// Implementación de IAuditoriaPublicador con el broker: arma un AuditLogMessage con Service = "Availability"
    /// (mismo formato que Payment). Un fallo al publicar se registra pero nunca se propaga.
    /// </summary>
    public sealed class AuditoriaPublicadorServiceBus(
        IMessagePublisher publisher, ILogger<AuditoriaPublicadorServiceBus> logger) : IAuditoriaPublicador
    {
        public Task IniciarProcesoAsync(string correlationId, string proceso,
            Dictionary<string, object?>? detalles = null, CancellationToken ct = default) =>
            PublicarAsync(correlationId, "InitProcess", $"Inicio del proceso {proceso}.",
                CrearDetalles(proceso, detalles), ct);

        public Task FinalizarProcesoAsync(string correlationId, string proceso, object? resultado,
            Dictionary<string, object?>? detalles = null, CancellationToken ct = default)
        {
            var d = CrearDetalles(proceso, detalles);
            d["Result"] = resultado;
            return PublicarAsync(correlationId, "EndProcess", $"Finalización del proceso {proceso}.", d, ct);
        }

        public Task EmitirErrorAsync(string correlationId, string proceso, Exception excepcion,
            Dictionary<string, object?>? detalles = null, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(excepcion);

            var d = CrearDetalles(proceso, detalles);
            d["ErrorType"] = excepcion.GetType().FullName;
            d["ErrorMessage"] = excepcion.Message;
            d["StackTrace"] = excepcion.StackTrace;
            return PublicarAsync(correlationId, "EmitError", $"Error en el proceso {proceso}: {excepcion.Message}", d, ct);
        }

        private static Dictionary<string, object?> CrearDetalles(string proceso, Dictionary<string, object?>? detalles)
        {
            // Copia para no modificar los datos del llamador.
            var d = detalles is null ? new Dictionary<string, object?>() : new Dictionary<string, object?>(detalles);
            d["ProcessName"] = proceso;
            return d;
        }

        private async Task PublicarAsync(string correlationId, string accion, string mensaje,
            Dictionary<string, object?> detalles, CancellationToken ct)
        {
            var auditLog = new AuditLogMessage(
                LogId: Guid.NewGuid(),
                DateUtc: DateTime.UtcNow,
                Service: "Availability",
                Action: accion,
                Message: mensaje,
                CorrelationId: correlationId,
                Details: detalles);

            try
            {
                await publisher.PublishAsync(auditLog, correlationId, ct);
            }
            catch (Exception ex)
            {
                // La auditoría no debe impedir la reserva ni reemplazar el error original.
                logger.LogError(ex, "No se pudo publicar la auditoría {Action}. CorrelationId={CorrelationId}",
                    accion, correlationId);
            }
        }
    }
}
