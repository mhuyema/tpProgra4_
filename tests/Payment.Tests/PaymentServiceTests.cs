using Payment.Business.Exceptions;
using Payment.Business.Services;
using Payment.Data.Entities;
using Payment.Data.Repositories;
using Xunit;
using PaymentEntity = Payment.Data.Entities.Payment;

namespace Payment.Tests;

public sealed class PaymentServiceTests
{
    private readonly PaymentRepository _repository = new();
    private PaymentService Service => new(_repository);

    [Theory]
    [InlineData("25.37", PaymentStatus.APPROVED)]
    [InlineData("0", PaymentStatus.REJECTED)]
    [InlineData("-10", PaymentStatus.REJECTED)]
    public async Task AmountDeterminesFinalStatus(string value, PaymentStatus expected)
    {
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        var payment = await Service.PayAsync(Guid.NewGuid(), amount, Guid.NewGuid());
        Assert.Equal(expected, payment.Status);
        Assert.NotEqual(Guid.Empty, payment.PaymentId);
        Assert.Equal(TimeSpan.Zero, payment.CreatedAt.Offset);
        Assert.Equal(expected, _repository.GetPayment(payment.PaymentId)!.Status);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EmptyIdentifiersAreRejected(bool emptyBooking)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Service.PayAsync(
            emptyBooking ? Guid.Empty : Guid.NewGuid(), 10,
            emptyBooking ? Guid.NewGuid() : Guid.Empty));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(0)]
    public async Task RepeatingAttemptReturnsOriginalPayment(int amount)
    {
        var service = Service;
        var booking = Guid.NewGuid();
        var attempt = Guid.NewGuid();
        var first = await service.PayAsync(booking, amount, attempt);
        var repeated = await service.PayAsync(booking, amount, attempt);
        Assert.Equal(first.PaymentId, repeated.PaymentId);
        Assert.Equal(first.Status, repeated.Status);
        Assert.Single(_repository.GetPaymentByBookingId(booking));
        await Assert.ThrowsAsync<PaymentConflictException>(() => service.PayAsync(booking, amount + 1, attempt));
    }

    [Fact]
    public async Task NewAttemptCanFollowRejectionButCannotFollowApproval()
    {
        var service = Service;
        var booking = Guid.NewGuid();
        await service.PayAsync(booking, 0, Guid.NewGuid());
        var approved = await service.PayAsync(booking, 10, Guid.NewGuid());
        Assert.Equal(PaymentStatus.APPROVED, approved.Status);
        await Assert.ThrowsAsync<PaymentConflictException>(() => service.PayAsync(booking, 10, Guid.NewGuid()));
        Assert.Equal(2, _repository.GetPaymentByBookingId(booking).Count);
    }

    [Fact]
    public async Task ProcessingAttemptIsReturnedAndOtherAttemptsAreBlocked()
    {
        var pending = new PaymentEntity { BookingId = Guid.NewGuid(), PaymentAttemptId = Guid.NewGuid(), Amount = 20 };
        _repository.CreatePayment(pending);
        var service = Service;
        var result = await service.PayAsync(pending.BookingId, pending.Amount, pending.PaymentAttemptId);
        Assert.Equal(pending.PaymentId, result.PaymentId);
        Assert.Equal(PaymentStatus.PROCESSING, result.Status);
        await Assert.ThrowsAsync<PaymentConflictException>(() =>
            service.PayAsync(pending.BookingId, 20, Guid.NewGuid()));
        await Assert.ThrowsAsync<PaymentConflictException>(() =>
            service.PayAsync(pending.BookingId, 21, pending.PaymentAttemptId));
        Assert.Single(_repository.GetPaymentByBookingId(pending.BookingId));
    }

    [Fact]
    public async Task ConcurrentRepeatedCallsCreateOnePayment()
    {
        var service = Service;
        var booking = Guid.NewGuid();
        var attempt = Guid.NewGuid();
        var payments = await Task.WhenAll(Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => service.PayAsync(booking, 10, attempt))));
        var stored = Assert.Single(_repository.GetPaymentByBookingId(booking));
        Assert.All(payments, p => Assert.Equal(stored.PaymentId, p.PaymentId));
        Assert.Equal(PaymentStatus.APPROVED, stored.Status);
    }

    [Fact]
    public async Task ConcurrentDifferentAttemptsHaveOneWinner()
    {
        var service = Service;
        var booking = Guid.NewGuid();
        var results = await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => Task.Run(async () =>
        {
            try { await service.PayAsync(booking, 10, Guid.NewGuid()); return true; }
            catch (PaymentConflictException) { return false; }
        })));
        Assert.Equal(1, results.Count(success => success));
        Assert.Single(_repository.GetPaymentByBookingId(booking));
    }
}
