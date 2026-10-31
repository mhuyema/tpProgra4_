namespace Broker.Messaging.Contracts;

/// <summary>
/// Mensaje de auditoría compartido entre los servicios. Es un mensaje anémico (solo datos).
/// </summary>
/// <param name="LogId">Identificador del registro de auditoría.</param>
/// <param name="DateUtc">Fecha y hora del registro en UTC.</param>
/// <param name="Service">Servicio que genera el registro.</param>
/// <param name="Action">Acción registrada.</param>
/// <param name="Message">Descripción del registro.</param>
/// <param name="CorrelationId">Identificador para correlacionar registros de una misma operación.</param>
/// <param name="Details">Datos adicionales según el registro, como información de un error o de una reserva.</param>
public record AuditLogMessage(
    Guid? LogId = null,
    DateTime? DateUtc = null,
    string? Service = null,
    string? Action = null,
    string? Message = null,
    string? CorrelationId = null,
    Dictionary<string, object?>? Details = null
);
