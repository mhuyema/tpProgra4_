using AuditLog.Api.Models;
using LiteDB;
using System.Text.Json;

namespace AuditLog.Api.Data;

public sealed class AuditLogRepository
{
    private readonly ILiteCollection<BsonDocument> _logs;

    public AuditLogRepository(ILiteDatabase database)
    {
        _logs = database.GetCollection("logs");
        foreach (var field in new[] { "DateUtc", "CorrelationId", "Action", "Service", "SortKey" })
            _logs.EnsureIndex(field);
    }

    public void Save(AuditLogEntry entry)
    {
        var document = new BsonDocument
        {
            ["_id"] = entry.LogId,
            ["DateUtc"] = entry.DateUtc,
            ["Service"] = entry.Service,
            ["Action"] = entry.Action,
            ["Message"] = entry.Message,
            ["CorrelationId"] = entry.CorrelationId,
            // JSON conserva los valores arbitrarios de Details, incluidos objetos y arreglos.
            ["DetailsJson"] = entry.Details?.GetRawText(),
            // Desempata por LogId cuando varios registros tienen la misma fecha.
            ["SortKey"] = $"{entry.DateUtc.Ticks:D19}:{entry.LogId:N}"
        };
        try
        {
            _logs.Insert(document);
        }
        catch (LiteException ex) when (ex.ErrorCode == LiteException.INDEX_DUPLICATE_KEY)
        {
            // Service Bus puede reentregar un mensaje ya guardado: conserva el original.
            if (_logs.FindById(entry.LogId) is null)
                throw;
        }
    }

    public IReadOnlyList<AuditLogEntry> Find(LogQuery filter)
    {
        var query = _logs.Query();
        var correlationId = filter.CorreletionId ?? filter.CorrelationId;
        if (correlationId is not null) query = query.Where(Query.EQ("CorrelationId", correlationId));
        if (filter.Action is not null) query = query.Where(Query.EQ("Action", filter.Action));
        if (filter.LogId.HasValue) query = query.Where(Query.EQ("_id", filter.LogId.Value));
        if (filter.Service is not null) query = query.Where(Query.EQ("Service", filter.Service));
        if (filter.StartDate.HasValue) query = query.Where(Query.GTE("DateUtc", filter.StartDate.Value.UtcDateTime));
        if (filter.EndDate.HasValue) query = query.Where(Query.LTE("DateUtc", filter.EndDate.Value.UtcDateTime));

        var sortField = filter.SortBy switch
        {
            "service" => "Service",
            "action" => "Action",
            "message" => "Message",
            "correlationId" => "CorrelationId",
            "logId" => "_id",
            _ => "SortKey"
        };
        return query.OrderBy(sortField, filter.SortDirection == "asc" ? Query.Ascending : Query.Descending)
            .Offset(checked((filter.Page - 1) * LogQuery.PageSize))
            .Limit(LogQuery.PageSize).ToList()
            .Select(document => new AuditLogEntry(
                document["_id"].AsGuid, document["DateUtc"].AsDateTime,
                document["Service"].AsString, document["Action"].AsString,
                document["Message"].AsString, document["CorrelationId"].AsString,
                document["DetailsJson"].IsNull ? null :
                    System.Text.Json.JsonSerializer.Deserialize<JsonElement>(document["DetailsJson"].AsString)))
            .ToList();
    }
}

