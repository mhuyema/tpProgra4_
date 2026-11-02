using System.Text.Json;

namespace AuditLog.Api.Models;

public record AuditLogEntry(
    Guid LogId,
    DateTime DateUtc,
    string? Service,
    string? Action,
    string? Message,
    string? CorrelationId,
    JsonElement? Details);
