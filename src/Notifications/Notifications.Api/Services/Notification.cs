namespace Notifications.Api.Services;

// Aviso enviado a un usuario. Dato puro, sin comportamiento (dominio anemico, igual que el resto del TP).
public record Notification(
    Guid RequestId,
    string Type,
    string UserId,
    string Message,
    DateTime SentAtUtc
);
