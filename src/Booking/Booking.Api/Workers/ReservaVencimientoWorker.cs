using Booking.Business.Auditing;
using Booking.Business.Services;

namespace Booking.Api.Workers;

// Paso 8 del flujo: una Retenida que supera VenceEn pasa Vencida y el asiento se libera
// Una retenida abandonada bloquearia el asiento para siempre.
// Es seguro con varias replicas: el update es condicional y atomico en la base,
// si dos replicas barren a la vez, cada fila solo cambia una vez.
public class ReservaVencimientoWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<ReservaVencimientoWorker> _logger;

    public ReservaVencimientoWorker(IServiceScopeFactory scopeFactory, IConfiguration config,
        ILogger<ReservaVencimientoWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var segundos = _config.GetValue("Vencimiento:IntervaloSegundos", 30);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(segundos));

        try
        {
            do
            {
                // Cada barrido recibe un identificador propio para relacionar
                // sus registros de inicio, finalización y posibles errores.
                // No se utiliza CorrelationId porque un barrido puede
                // procesar varias solicitudes con identificadores diferentes.
                var barridoId = Guid.NewGuid();

                using var logScope = _logger.BeginScope(
                    new Dictionary<string, object>
                    {
                        ["BarridoId"] = barridoId
                    });

                try
                {
                    _logger.LogInformation(
                        "[AUDITORIA] INICIO de barrido de vencimientos. BarridoId={BarridoId}",
                        barridoId);

                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IReservaService>();

                    // El servicio registra individualmente cada vencimiento
                    // aplicado, utilizando SolicitudId y ReservaId.
                    var cantidad = await service.VencerReservasAsync();

                    _logger.LogInformation(
                        "[AUDITORIA] FIN de barrido de vencimientos. BarridoId={BarridoId} CantidadVencidas={Cantidad}",
                        barridoId,
                        cantidad);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Un fallo puntual no debe matar el worker: se reintenta en el proximo ciclo
                    _logger.LogError(ex, "[AUDITORIA] Fallo el barrido de reservas vencidas");

                    using var errorScope = _scopeFactory.CreateScope();
                    await errorScope.ServiceProvider.GetRequiredService<IBookingAuditPublisher>()
                        .EmitErrorAsync(barridoId.ToString(), "BarridoVencimientos", ex);
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) { /* apagado normal */ }
    }
}