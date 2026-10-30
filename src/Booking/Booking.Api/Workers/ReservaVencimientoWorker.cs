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
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IReservaService>();
                    await service.VencerReservasAsync();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Un fallo puntual no debe matar el worker: se reintenta en el proximo ciclo
                    _logger.LogError(ex, "[AUDITORIA] Fallo el barrido de reservas vencidas");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) { /* apagado normal */ }
    }
}
