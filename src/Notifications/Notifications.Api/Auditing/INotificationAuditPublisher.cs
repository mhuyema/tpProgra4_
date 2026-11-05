namespace Notifications.Api.Auditing;

// ---------------------------------------------------------------------------------------------
// ¿Para qué sirve este archivo?
// El TP (punto 3.f) pide "logs de auditoría que identifiquen el inicio y el fin de una cadena de
// sucesos en cada microservicio". Booking, Payment, Authorization y Availability ya lo hacen:
// mandan un mensaje de auditoría al broker y el servicio AuditLog lo guarda.
// Notifications era el único que solo escribía en la consola; con esta interfaz se suma a los demás.
//
// ¿Por qué una INTERFAZ y no usar directamente la clase?
// Porque los handlers van a depender de esta "promesa" (el contrato) y no de la implementación real.
// Así, en los tests podemos pasarles una versión falsa que no necesita broker. Es el mismo truco
// que usan IPaymentAuditPublisher (Payment) y IBookingAuditPublisher (Booking).
// ---------------------------------------------------------------------------------------------
public interface INotificationAuditPublisher
{
    // Se llama al EMPEZAR un proceso (por ejemplo "NotificarReservaCreada").
    Task InitProcessAsync(string correlationId, string processName,
        Dictionary<string, object?>? details = null, CancellationToken cancellationToken = default);

    // Se llama al TERMINAR bien. "result" cuenta cómo terminó (por ejemplo "Enviada" o "Duplicada").
    Task EndProcessAsync(string correlationId, string processName, object? result,
        Dictionary<string, object?>? details = null, CancellationToken cancellationToken = default);

    // Se llama cuando algo SALIÓ MAL (una excepción o un mensaje inválido).
    Task EmitErrorAsync(string correlationId, string processName, Exception exception,
        Dictionary<string, object?>? details = null, CancellationToken cancellationToken = default);
}