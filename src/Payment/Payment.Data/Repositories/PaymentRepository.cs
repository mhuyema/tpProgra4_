using Payment.Data.Entities;
using PaymentEntity = Payment.Data.Entities.Payment;

namespace Payment.Data.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, PaymentEntity> _payments = new();

    public void CreatePayment(PaymentEntity payment)
    {
        lock (_sync)
        {
            if (_payments.ContainsKey(payment.PaymentId) || _payments.Values.Any(p =>
                p.BookingId == payment.BookingId && p.PaymentAttemptId == payment.PaymentAttemptId))
                throw new InvalidOperationException("El pago o intento ya existe.");
            if (payment.Status != PaymentStatus.PROCESSING)
                throw new InvalidOperationException("Un pago nuevo debe estar PROCESSING.");
            _payments.Add(payment.PaymentId, Copy(payment));
        }
    }

    public PaymentEntity? GetPayment(Guid paymentId)
    {
        lock (_sync)
            return _payments.TryGetValue(paymentId, out var payment) ? Copy(payment) : null;
    }

    public IReadOnlyList<PaymentEntity> GetPaymentByBookingId(Guid bookingId)
    {
        lock (_sync)
            return _payments.Values.Where(p => p.BookingId == bookingId).Select(Copy).ToArray();
    }

    public PaymentEntity? GetPaymentByAttempt(Guid bookingId, Guid paymentAttemptId)
    {
        lock (_sync)
        {
            var payment = _payments.Values.FirstOrDefault(p =>
                p.BookingId == bookingId && p.PaymentAttemptId == paymentAttemptId);
            return payment is null ? null : Copy(payment);
        }
    }

    public PaymentEntity? UpdatePaymentStatus(Guid paymentId, PaymentStatus status)
    {
        lock (_sync)
        {
            if (!_payments.TryGetValue(paymentId, out var payment)) return null;
            if (payment.Status != PaymentStatus.PROCESSING ||
                status is not (PaymentStatus.APPROVED or PaymentStatus.REJECTED))
                throw new InvalidOperationException("Solo se permite pasar de PROCESSING a APPROVED o REJECTED.");
            payment.Status = status;
            return Copy(payment);
        }
    }

    // Las copias impiden modificar el estado almacenado sin pasar por el repositorio.
    private static PaymentEntity Copy(PaymentEntity payment) => new()
    {
        PaymentId = payment.PaymentId,
        BookingId = payment.BookingId,
        PaymentAttemptId = payment.PaymentAttemptId,
        Amount = payment.Amount,
        Status = payment.Status,
        CreatedAt = payment.CreatedAt
    };
}
