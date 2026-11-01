using Microsoft.Extensions.Logging.Abstractions;
using Payment.Business.Auditing;
using Payment.Business.Exceptions;
using Payment.Business.Services;
using Payment.Data;
using Payment.Data.Entities;
using Payment.Data.Repositories;
using Xunit;
using PaymentEntity = Payment.Data.Entities.Payment;

namespace Payment.Tests;

[Collection(PaymentDatabaseCollection.Name)]
public sealed class PaymentServiceTests : IDisposable
{
    private readonly PaymentDatabaseFixture _database;
    private readonly AppDbContext _db;
    private readonly PaymentRepository _repository;
    private readonly TestMessagePublisher _publisher = new();
    private PaymentService Service => CreateService(_repository);

    private PaymentService CreateService(IPaymentRepository repository) => new(
        repository, new PaymentAuditPublisher(_publisher, NullLogger<PaymentAuditPublisher>.Instance));

    public PaymentServiceTests(PaymentDatabaseFixture database)
    {
        _database = database;
        _db = database.CreateDbContext();
        _repository = new PaymentRepository(_db);
    }

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
        var events = _publisher.Messages.ToArray();
        Assert.Equal(new[] { "InitProcess", "EndProcess" }, events.Select(e => e.Action));
        Assert.True(Guid.TryParse(events[0].CorrelationId, out _));
        Assert.Equal(events[0].CorrelationId, events[1].CorrelationId);
        var result = System.Text.Json.JsonSerializer.SerializeToElement(events[1].Details!["Result"]);
        Assert.Equal(payment.BookingId, result.GetProperty("BookingId").GetGuid());
        Assert.Equal(payment.PaymentId, result.GetProperty("PaymentId").GetGuid());
        Assert.Equal(expected.ToString(), result.GetProperty("Status").GetString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EmptyIdentifiersAreRejected(bool emptyBooking)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Service.PayAsync(
            emptyBooking ? Guid.Empty : Guid.NewGuid(), 10,
            emptyBooking ? Guid.NewGuid() : Guid.Empty));
        var events = _publisher.Messages.ToArray();
        Assert.Equal(new[] { "InitProcess", "EmitError" }, events.Select(e => e.Action));
        Assert.Equal(events[0].CorrelationId, events[1].CorrelationId);
        Assert.Equal(typeof(ArgumentException).FullName, events[1].Details!["ErrorType"]);
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
        var events = _publisher.Messages.ToArray();
        Assert.Equal(new[] { "InitProcess", "EndProcess", "InitProcess", "EndProcess" },
            events.Select(e => e.Action));
        Assert.NotEqual(events[0].CorrelationId, events[2].CorrelationId);
        await Assert.ThrowsAsync<PaymentConflictException>(() => service.PayAsync(booking, amount + 1, attempt));
        Assert.Equal("EmitError", _publisher.Messages.Last().Action);
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
        var booking = Guid.NewGuid();
        var attempt = Guid.NewGuid();
        var payments = await Task.WhenAll(Enumerable.Range(0, 100)
            .Select(_ => Task.Run(async () =>
            {
                using var db = _database.CreateDbContext();
                var service = CreateService(new PaymentRepository(db));
                return await service.PayAsync(booking, 10, attempt);
            })));
        var stored = Assert.Single(_repository.GetPaymentByBookingId(booking));
        Assert.All(payments, p => Assert.Equal(stored.PaymentId, p.PaymentId));
        Assert.Equal(PaymentStatus.APPROVED, stored.Status);
    }

    [Fact]
    public async Task ConcurrentDifferentAttemptsHaveOneWinner()
    {
        var booking = Guid.NewGuid();
        var results = await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => Task.Run(async () =>
        {
            using var db = _database.CreateDbContext();
            var service = CreateService(new PaymentRepository(db));
            try { await service.PayAsync(booking, 10, Guid.NewGuid()); return true; }
            catch (PaymentConflictException) { return false; }
        })));
        Assert.Equal(1, results.Count(success => success));
        Assert.Single(_repository.GetPaymentByBookingId(booking));
    }

    [Theory]
    [InlineData(10, PaymentStatus.APPROVED)]
    [InlineData(0, PaymentStatus.REJECTED)]
    public async Task BrokerFailureDoesNotPreventPayment(int amount, PaymentStatus expected)
    {
        var service = new PaymentService(_repository, new PaymentAuditPublisher(
            new TestMessagePublisher { FailPublishing = true }, NullLogger<PaymentAuditPublisher>.Instance));

        var payment = await service.PayAsync(Guid.NewGuid(), amount, Guid.NewGuid());

        Assert.Equal(expected, payment.Status);
        Assert.Equal(expected, _repository.GetPayment(payment.PaymentId)!.Status);
    }

    [Fact]
    public async Task BrokerFailureDoesNotReplaceOriginalPaymentError()
    {
        var service = new PaymentService(_repository, new PaymentAuditPublisher(
            new TestMessagePublisher { FailPublishing = true }, NullLogger<PaymentAuditPublisher>.Instance));

        var error = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.PayAsync(Guid.Empty, 10, Guid.NewGuid()));

        Assert.Equal("bookingId", error.ParamName);
    }

    public void Dispose() => _db.Dispose();
}
