using Azure.Messaging.ServiceBus;
using Broker.Messaging.Abstractions;
using Broker.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Broker.Messaging.DependencyInjection;

public static class BrokerServiceCollectionExtensions
{
    /// <summary>
    /// Registra el cliente del broker. Usar en todo servicio que publique o consuma.
    /// Falla al arrancar (fail fast) si falta el connection string, en vez de fallar en el primer mensaje.
    /// </summary>
    public static IServiceCollection AddBroker(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BrokerOptions>(configuration.GetSection(BrokerOptions.SectionName));

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BrokerOptions>>().Value;
            var connectionString = options.ConnectionString
                ?? Environment.GetEnvironmentVariable(BrokerOptions.ConnectionStringEnvVar);
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    $"Falta el connection string del broker: definir la variable de entorno {BrokerOptions.ConnectionStringEnvVar}.");
            return new ServiceBusClient(connectionString);
        });
        return services;
    }

    /// <summary>Habilita IMessagePublisher (Availability).</summary>
    public static IServiceCollection AddBrokerPublisher(this IServiceCollection services)
    {
        services.AddSingleton<IMessagePublisher, ServiceBusMessagePublisher>();
        return services;
    }

    /// <summary>
    /// Habilita el consumo de TMessage desde la suscripción indicada (Booking: "booking").
    /// THandler se resuelve por DI en cada mensaje, hay que registrarlo (AddScoped).
    /// </summary>
    public static IServiceCollection AddBrokerSubscriber<TMessage, THandler>(
        this IServiceCollection services, string subscriptionName)
        where THandler : class, IMessageHandler<TMessage>
    {
        services.AddHostedService(sp => new ServiceBusSubscriberService<TMessage, THandler>(
            sp.GetRequiredService<ServiceBusClient>(),
            sp.GetRequiredService<IOptions<BrokerOptions>>(),
            subscriptionName,
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<ILogger<ServiceBusSubscriberService<TMessage, THandler>>>()));
        return services;
    }
}
