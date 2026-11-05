using Notifications.Api.Auditing;
using Notifications.Api.Auth;
using Notifications.Api.Services;

namespace Notifications.Tests;

// ---------------------------------------------------------------------------------------------
// Dobles de prueba ("fakes"): versiones falsas y simples de las dependencias de los handlers.
// Nos dejan probar la lógica de Notifications SIN broker, SIN Authorization Server y SIN enviar nada real.
// Esto es posible porque los handlers dependen de INTERFACES (inyección de dependencias).
// ---------------------------------------------------------------------------------------------

// Se anota todo lo que le piden enviar, en vez de enviar un mail de verdad.
internal sealed class FakeSender : INotificationSender
{
    public List<Notification> Sent { get; } = new();

    // Si es true, simula que el canal de envío (email, SMS...) se cayó.
    public bool Fail { get; init; }

    public Task SendAsync(Notification notification)
    {
        if (Fail)
            throw new InvalidOperationException("El canal de envío no está disponible.");

        Sent.Add(notification);
        return Task.CompletedTask;
    }
}

// Guarda en una lista los eventos de auditoría ("Init", "End", "Error") para poder revisarlos en los tests.
internal sealed class FakeAuditPublisher : INotificationAuditPublisher
{
    public List<(string Action, string ProcessName, object? Result)> Events { get; } = new();

    public Task InitProcessAsync(string correlationId, string processName,
        Dictionary<string, object?>? details = null, CancellationToken cancellationToken = default)
    {
        Events.Add(("Init", processName, null));
        return Task.CompletedTask;
    }

    public Task EndProcessAsync(string correlationId, string processName, object? result,
        Dictionary<string, object?>? details = null, CancellationToken cancellationToken = default)
    {
        Events.Add(("End", processName, result));
        return Task.CompletedTask;
    }

    public Task EmitErrorAsync(string correlationId, string processName, Exception exception,
        Dictionary<string, object?>? details = null, CancellationToken cancellationToken = default)
    {
        Events.Add(("Error", processName, exception.Message));
        return Task.CompletedTask;
    }
}

// Valida tokens "de mentira": solo acepta el token que le configuramos y le asigna el usuario que queramos.
internal sealed class FakeValidadorToken : IValidadorTokenUsuario
{
    public string? TokenValido { get; init; }
    public string? Subject { get; init; }

    // Si es true, simula que el Authorization Server no responde.
    public bool Caido { get; init; }

    public Task<ResultadoTokenUsuario> ValidarAsync(string? token, CancellationToken cancellationToken = default)
    {
        if (Caido)
            throw new HttpRequestException("Authorization Server caído.");

        return Task.FromResult(token is not null && token == TokenValido
            ? new ResultadoTokenUsuario(true, Subject)
            : ResultadoTokenUsuario.Invalido);
    }
}
