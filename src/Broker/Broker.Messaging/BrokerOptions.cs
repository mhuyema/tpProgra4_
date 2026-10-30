namespace Broker.Messaging;

/// <summary>
/// Configuración del broker (sección "Broker" de appsettings.json).
/// El connection string NUNCA va en el repo: se lee de la variable de entorno SERVICEBUS_CONNECTION_STRING.
/// </summary>
public class BrokerOptions
{
    public const string SectionName = "Broker";
    public const string ConnectionStringEnvVar = "SERVICEBUS_CONNECTION_STRING";

    public string? ConnectionString { get; set; }

    /// <summary>Topic del grupo 9 en Azure Service Bus (el connection string de la cátedra está atado a esta entidad).</summary>
    public string TopicName { get; set; } = "grupo9";

    /// <summary>Cantidad de mensajes procesados en paralelo por cada suscriptor.</summary>
    public int MaxConcurrentCalls { get; set; } = 4;
}
