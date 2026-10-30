using Booking.Business.Services;
using Booking.Data.Repositories;

namespace Booking.Api.Workers;

// Recupera periódicamente los intentos pendientes de Payment.
// Utiliza scopes independientes para resolver servicios Scoped
// desde un BackgroundService, que permanece activo como singleton.
public sealed class PagosPendientesWorker : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PagosPendientesWorker> _logger;

    public PagosPendientesWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<PagosPendientesWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcesarPendientesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Una falla del broker, de Payment o de la base no debe
                // detener permanentemente el trabajador de recuperación.
                _logger.LogError(
                    ex,
                    "[AUDITORIA] Error al recuperar pagos pendientes");
            }

            try
            {
                await Task.Delay(Intervalo, stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcesarPendientesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> solicitudes;

        // Consulta las solicitudes pendientes con un contexto propio.
        using (var scope = _scopeFactory.CreateScope())
        {
            var repositorio = scope.ServiceProvider
                .GetRequiredService<IReservaRepository>();

            solicitudes = await repositorio
                .ObtenerSolicitudesConPagoPendienteAsync(DateTime.UtcNow);
        }

        foreach (var solicitudId in solicitudes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Recupera la correlación original de la solicitud.
            // El scope también acompaña los logs emitidos por ReservaService.
            using var logScope = _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["CorrelationId"] = solicitudId.ToString()
                });

            try
            {
                // Cada solicitud utiliza un scope independiente.
                // ProcesarPagoAsync reutiliza el intento persistido
                // y comprueba nuevamente el estado y vencimiento.
                using var scope = _scopeFactory.CreateScope();

                var servicio = scope.ServiceProvider
                    .GetRequiredService<IReservaService>();

                // Auditoría: identifica la recuperación automática
                // de una solicitud previamente iniciada.
                _logger.LogInformation(
                    "[AUDITORIA] Recuperación automática de pago pendiente. SolicitudId={SolicitudId}",
                    solicitudId);

                await servicio.ProcesarPagoAsync(solicitudId);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Un pago problemático no interrumpe la recuperación
                // del resto de las solicitudes pendientes.
                _logger.LogError(
                    ex,
                    "[AUDITORIA] No se pudo recuperar el pago de la solicitud {SolicitudId}",
                    solicitudId);
            }
        }
    }
}