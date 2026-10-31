using Payment.Data.Entities;
using Payment.Data.Exceptions;
using Payment.Data.Repositories;
using Xunit;
using PaymentEntity = Payment.Data.Entities.Payment;

namespace Payment.Tests;

[Collection(PaymentDatabaseCollection.Name)]
public sealed class PaymentRepositoryTests(PaymentDatabaseFixture database)
{
    [Fact]
    public void MissingPaymentsReturnNullOrEmptyList()
    {
        using var db = database.CreateDbContext();
        var repository = new PaymentRepository(db);
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
        using var db = database.CreateDbContext();
        var repository = new PaymentRepository(db);
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
        using var db = database.CreateDbContext();
        var repository = new PaymentRepository(db);
        var payment = new PaymentEntity { BookingId = Guid.NewGuid(), PaymentAttemptId = Guid.NewGuid() };
        repository.CreatePayment(payment);
        payment.Status = PaymentStatus.REJECTED;
        repository.GetPayment(payment.PaymentId)!.Status = PaymentStatus.APPROVED;
        repository.GetPaymentByAttempt(payment.BookingId, payment.PaymentAttemptId)!.Status = PaymentStatus.REJECTED;
        repository.GetPaymentByBookingId(payment.BookingId)[0].Status = PaymentStatus.APPROVED;
        Assert.Equal(PaymentStatus.PROCESSING, repository.GetPayment(payment.PaymentId)!.Status);
        Assert.Throws<DuplicatePaymentException>(() => repository.CreatePayment(new PaymentEntity
        { BookingId = payment.BookingId, PaymentAttemptId = payment.PaymentAttemptId }));
    }

    [Fact]
    public void PaymentsRemainAvailableInAnotherContext()
    {
        var payment = new PaymentEntity
        {
            BookingId = Guid.NewGuid(),
            PaymentAttemptId = Guid.NewGuid(),
            Amount = 25.37m
        };
        using (var db = database.CreateDbContext())
        {
            var repository = new PaymentRepository(db);
            repository.CreatePayment(payment);
            repository.UpdatePaymentStatus(payment.PaymentId, PaymentStatus.APPROVED);
        }

        using var anotherDb = database.CreateDbContext();
        var stored = new PaymentRepository(anotherDb)
            .GetPaymentByAttempt(payment.BookingId, payment.PaymentAttemptId);
        Assert.NotNull(stored);
        Assert.Equal(payment.PaymentId, stored.PaymentId);
        Assert.Equal(payment.Amount, stored.Amount);
        Assert.Equal(PaymentStatus.APPROVED, stored.Status);
    }
}
