using PaymentEntity = Payment.Data.Entities.Payment;

namespace Payment.Business.Services;

public interface IPaymentService
{
    Task<PaymentEntity> PayAsync(Guid bookingId, decimal amount, Guid paymentAttemptId);
}
