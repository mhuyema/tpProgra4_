using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Broker.Messaging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Broker.Messaging.ServiceBus;

/// <summary>
/// Consumidor de una suscripción del topic, corre como BackgroundService dentro del microservicio.
///
/// Patrón Competing Consumers: todas las réplicas de Booking usan la MISMA suscripción, entonces
/// el broker entrega cada mensaje a una sola de ellas (requisito: "una de las réplicas toma el mensaje").
///
/// Entrega at-least-once: el mensaje se confirma (Complete) recién después de procesarlo OK.
/// Si el handler falla se abandona (Abandon) y el broker lo reentrega; superado el MaxDeliveryCount
/// configurado en la suscripción pasa a la dead-letter queue en vez de perderse o ciclar para siempre.
/// </summary>
public sealed class ServiceBusSubscriberService<TMessage, THandler> : BackgroundService
    where THandler : IMessageHandler<TMessage>
{
    private readonly ServiceBusProcessor _processor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ServiceBusSubscriberService<TMessage, THandler>> _logger;

    public ServiceBusSubscriberService(ServiceBusClient client, IOptions<BrokerOptions> options,
        string subscriptionName, IServiceScopeFactory scopeFactory,
        ILogger<ServiceBusSubscriberService<TMessage, THandler>> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _processor = client.CreateProcessor(options.Value.TopicName, subscriptionName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false, // confirmamos a mano, solo si el handler terminó bien
            MaxConcurrentCalls = options.Value.MaxConcurrentCalls
        });
        _processor.ProcessMessageAsync += OnMessageAsync;
        _processor.ProcessErrorAsync += OnErrorAsync;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _processor.StartProcessingAsync(stoppingToken);
        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) { /* apagado normal */ }
        await _processor.StopProcessingAsync(CancellationToken.None);
    }

    private async Task OnMessageAsync(ProcessMessageEventArgs args)
    {
        var correlationId = args.Message.CorrelationId ?? args.Message.MessageId;

        _logger.LogInformation("[AUDITORIA] RECIBIDO correlationId={CorrelationId} intento={Intento}",
            correlationId, args.Message.DeliveryCount);

        var message = JsonSerializer.Deserialize<TMessage>(args.Message.Body.ToArray());
        if (message is null)
        {
            // Un mensaje ilegible nunca va a poder procesarse: a la dead-letter queue, sin reintentos.
            await args.DeadLetterMessageAsync(args.Message, "Deserializacion",
                "El cuerpo no se pudo deserializar", args.CancellationToken);
            _logger.LogError("[AUDITORIA] FIN (dead-letter) correlationId={CorrelationId}", correlationId);
            return;
        }

        // Un scope por mensaje: así el handler puede usar servicios scoped (DbContext, etc.).
        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = (IMessageHandler<TMessage>)scope.ServiceProvider.GetService(typeof(THandler))!;

        try
        {
            await handler.HandleAsync(message, correlationId, args.CancellationToken);
            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
            _logger.LogInformation("[AUDITORIA] FIN correlationId={CorrelationId} procesado OK", correlationId);
        }
        catch (Exception ex)
        {
            await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
            _logger.LogWarning(ex, "[AUDITORIA] FIN (reintento) correlationId={CorrelationId} fallo, vuelve al broker",
                correlationId);
        }
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        // Errores del propio broker (conexión caída, permisos, etc.), no del handler.
        _logger.LogError(args.Exception, "Error del broker en {Origen} ({Namespace}/{Entity})",
            args.ErrorSource, args.FullyQualifiedNamespace, args.EntityPath);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await _processor.DisposeAsync();
    }
}
