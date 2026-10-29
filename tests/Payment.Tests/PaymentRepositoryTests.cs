using Payment.Data.Entities;
using Payment.Data.Repositories;
using Xunit;
using PaymentEntity = Payment.Data.Entities.Payment;

namespace Payment.Tests;

public sealed class PaymentRepositoryTests
{
    [Fact]
    public void MissingPaymentsReturnNullOrEmptyList()
    {
        var repository = new PaymentRepository();
        Assert.Null(repository.GetPayment(Guid.NewGuid()));
        Assert.Null(repository.GetPaymentByAttempt(Guid.NewGuid(), Guid.NewGuid()));
        Assert.Null(repository.UpdatePaymentStatus(Guid.NewGuid(), PaymentStatus.APPROVED));
        Assert.Empty(repository.GetPaymentByBookingId(Guid.NewGuid()));
    }

    [Theory]
    [InlineData(PaymentStatus.APPROVED)]
    [InlineData(PaymentStatus.REJECTED)]
    public void OnlyProcessingCanTransitionToFinalState(PaymentStatus finalStatus)
    {
        var repository = new PaymentRepository();
        var payment = new PaymentEntity { BookingId = Guid.NewGuid(), PaymentAttemptId = Guid.NewGuid() };
        repository.CreatePayment(payment);
        Assert.Throws<InvalidOperationException>(() => repository.UpdatePaymentStatus(payment.PaymentId, PaymentStatus.PROCESSING));
        Assert.Throws<InvalidOperationException>(() => repository.UpdatePaymentStatus(payment.PaymentId, (PaymentStatus)99));
        Assert.Equal(finalStatus, repository.UpdatePaymentStatus(payment.PaymentId, finalStatus)!.Status);
        Assert.Throws<InvalidOperationException>(() => repository.UpdatePaymentStatus(payment.PaymentId, finalStatus));
    }

    [Fact]
    public void ReturnedEntitiesCannotMutateStoredState()
    {
        var repository = new PaymentRepository();
        var payment = new PaymentEntity { BookingId = Guid.NewGuid(), PaymentAttemptId = Guid.NewGuid() };
        repository.CreatePayment(payment);
        payment.Status = PaymentStatus.REJECTED;
        repository.GetPayment(payment.PaymentId)!.Status = PaymentStatus.APPROVED;
        repository.GetPaymentByAttempt(payment.BookingId, payment.PaymentAttemptId)!.Status = PaymentStatus.REJECTED;
        repository.GetPaymentByBookingId(payment.BookingId)[0].Status = PaymentStatus.APPROVED;
        Assert.Equal(PaymentStatus.PROCESSING, repository.GetPayment(payment.PaymentId)!.Status);
        Assert.Throws<InvalidOperationException>(() => repository.CreatePayment(new PaymentEntity
        { BookingId = payment.BookingId, PaymentAttemptId = payment.PaymentAttemptId }));
    }
}
