namespace Notifications.Api.Services;

// El TP no pide un proveedor real de email o SMS: el envio se simula registrando el aviso en el log
// de auditoria, con el RequestId como correlationId para seguir la cadena completa de la reserva.
public sealed class SimulatedNotificationSender : INotificationSender
{
    private readonly ILogger<SimulatedNotificationSender> _logger;

    public SimulatedNotificationSender(ILogger<SimulatedNotificationSender> logger) => _logger = logger;

    public Task SendAsync(Notification notification)
    {
        _logger.LogInformation(
            "[AUDITORIA] NOTIFICACION enviada. correlationId={RequestId} tipo={Tipo} usuario={UserId} mensaje=\"{Mensaje}\"",
            notification.RequestId, notification.Type, notification.UserId, notification.Message);

        return Task.CompletedTask;
    }
}
