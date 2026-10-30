using Payment.Data.Entities;
using PaymentEntity = Payment.Data.Entities.Payment;

namespace Payment.Data.Repositories;

public interface IPaymentRepository
{
    void CreatePayment(PaymentEntity payment);
    PaymentEntity? GetPayment(Guid paymentId);
    IReadOnlyList<PaymentEntity> GetPaymentByBookingId(Guid bookingId);
    PaymentEntity? GetPaymentByAttempt(Guid bookingId, Guid paymentAttemptId);
    PaymentEntity? UpdatePaymentStatus(Guid paymentId, PaymentStatus status);
}
