namespace Notifications.Api.Services;

public interface INotificationStore
{
    // true = el aviso es nuevo y hay que enviarlo; false = ya estaba registrado (re-entrega del broker).
    bool TryAdd(Notification notification);

    IReadOnlyList<Notification> GetByUser(string userId);
}
