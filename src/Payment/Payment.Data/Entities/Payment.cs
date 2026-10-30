namespace Payment.Data.Entities;

// Dominio anémico: las reglas viven en PaymentService.
public sealed class Payment
{
    public Guid PaymentId { get; init; } = Guid.NewGuid();
    public Guid BookingId { get; init; }
    public Guid PaymentAttemptId { get; init; }
    public decimal Amount { get; init; }
    public PaymentStatus Status { get; set; } = PaymentStatus.PROCESSING;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
