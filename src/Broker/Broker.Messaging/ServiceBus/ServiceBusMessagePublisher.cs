using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Broker.Messaging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Broker.Messaging.ServiceBus;

/// <summary>
/// Patrón Publish/Subscribe: el publicador no conoce a los consumidores, solo escribe en el topic.
/// Availability puede responder "solicitud recibida" apenas se publica, sin esperar a Booking:
/// de ahí viene la consistencia eventual del sistema (3.g).
/// </summary>
public sealed class ServiceBusMessagePublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly ServiceBusSender _sender;
    private readonly ILogger<ServiceBusMessagePublisher> _logger;

    // ServiceBusClient es thread-safe y caro de crear: se registra como singleton y se reutiliza.
    public ServiceBusMessagePublisher(ServiceBusClient client, IOptions<BrokerOptions> options,
        ILogger<ServiceBusMessagePublisher> logger)
    {
        _sender = client.CreateSender(options.Value.TopicName);
        _logger = logger;
    }

    public async Task PublishAsync<T>(T message, string correlationId, CancellationToken cancellationToken = default)
    {
        var busMessage = new ServiceBusMessage(JsonSerializer.SerializeToUtf8Bytes(message))
        {
            // MessageId = correlationId + tipo: si el topic tiene habilitada la detección de duplicados (hoy no lo está),
            // reintentar la publicación de un mismo mensaje no lo duplica. El tipo evita que los distintos mensajes
            // de una misma cadena (solicitud, reserva creada, reserva confirmada) compartan MessageId y el broker
            // descarte los posteriores como si fueran duplicados.
            MessageId = $"{correlationId}:{typeof(T).Name}",
            CorrelationId = correlationId,
            Subject = typeof(T).Name,
            ContentType = "application/json"
        };

        // Log de auditoría (3.f): inicio de la cadena de sucesos.
        _logger.LogInformation("[AUDITORIA] INICIO cadena correlationId={CorrelationId} publicando {Tipo}",
            correlationId, typeof(T).Name);

        await _sender.SendMessageAsync(busMessage, cancellationToken);

        _logger.LogInformation("[AUDITORIA] PUBLICADO correlationId={CorrelationId} {Tipo} en el broker",
            correlationId, typeof(T).Name);
    }

    public ValueTask DisposeAsync() => _sender.DisposeAsync();
}
