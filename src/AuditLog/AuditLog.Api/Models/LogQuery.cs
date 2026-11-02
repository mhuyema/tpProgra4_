using System.ComponentModel.DataAnnotations;

namespace AuditLog.Api.Models;

public class LogQuery : IValidatableObject
{
    // Se admite tanto el nombre solicitado (correletionId) como correlationId.
    public string? CorreletionId { get; set; }
    public string? CorrelationId { get; set; }
    public string? Action { get; set; }
    public Guid? LogId { get; set; }
    public string? Service { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }

    [RegularExpression("^(dateUtc|service|action|message|correlationId|logId)$")]
    public string SortBy { get; set; } = "dateUtc";

    [RegularExpression("^(asc|desc)$")]
    public string SortDirection { get; set; } = "desc";

    [Range(1, int.MaxValue / PageSize)]
    public int Page { get; set; } = 1;

    public const int PageSize = 50;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate.HasValue && EndDate.HasValue && StartDate > EndDate)
            yield return new ValidationResult("startDate debe ser anterior o igual a endDate.",
                [nameof(StartDate), nameof(EndDate)]);
    }
}

