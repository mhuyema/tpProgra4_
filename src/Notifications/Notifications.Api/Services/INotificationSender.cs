namespace Notifications.Api.Services;

// Puerto de salida: como se le informa al usuario. Los handlers dependen de esta interfaz y no del canal
// concreto (email, SMS, push), asi se puede cambiar sin tocar la logica.
public interface INotificationSender
{
    Task SendAsync(Notification notification);
}
