using System.Collections.Concurrent;

namespace Notifications.Api.Services;

// Registro en memoria de los avisos enviados. La clave es (RequestId, Type): el broker entrega al menos una
// vez, asi que el mismo aviso puede llegar dos veces y no hay que avisarle dos veces al usuario (idempotencia).
// Limitacion asumida para el TP: al reiniciar el servicio se pierde el registro.
public sealed class InMemoryNotificationStore : INotificationStore
{
    private readonly ConcurrentDictionary<(Guid RequestId, string Type), Notification> _items = new();

    public bool TryAdd(Notification notification) =>
        _items.TryAdd((notification.RequestId, notification.Type), notification);

    public IReadOnlyList<Notification> GetByUser(string userId) =>
        _items.Values
            .Where(n => n.UserId == userId)
            .OrderBy(n => n.SentAtUtc)
            .ToList();
}
