using Microsoft.Extensions.Logging.Abstractions;
using Payment.Business.Auditing;
using Payment.Business.Exceptions;
using Payment.Business.Services;
using Payment.Data;
using Payment.Data.Entities;
using Payment.Data.Repositories;
using Microsoft.Extensions.Configuration;
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
        repository, new PaymentAuditPublisher(_publisher, NullLogger<PaymentAuditPublisher>.Instance),
        new ConfigurationBuilder().Build());

    public PaymentServiceTests(PaymentDatabaseFixture database)
    {
        _database = database;
        _db = database.CreateDbContext();
        _repository = new PaymentRepository(_db);
    }

    [Theory]
    [InlineData("25.37")]
    [InlineData("0.01")]
    public async Task PositiveAmountCreatesPaymentWithFinalStatus(string value)
    {
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        var payment = await Service.PayAsync(Guid.NewGuid(), amount, Guid.NewGuid());
        Assert.Contains(payment.Status, new[] { PaymentStatus.APPROVED, PaymentStatus.REJECTED });
        Assert.Equal(amount, payment.Amount);
        Assert.NotEqual(Guid.Empty, payment.PaymentId);
        Assert.Equal(TimeSpan.Zero, payment.CreatedAt.Offset);
        Assert.Equal(payment.Status, _repository.GetPayment(payment.PaymentId)!.Status);
        var events = _publisher.Messages.ToArray();
        Assert.Equal(new[] { "InitProcess", "EndProcess" }, events.Select(e => e.Action));
        Assert.True(Guid.TryParse(events[0].CorrelationId, out _));
        Assert.Equal(events[0].CorrelationId, events[1].CorrelationId);
        var result = System.Text.Json.JsonSerializer.SerializeToElement(events[1].Details!["Result"]);
        Assert.Equal(payment.BookingId, result.GetProperty("BookingId").GetGuid());
        Assert.Equal(payment.PaymentId, result.GetProperty("PaymentId").GetGuid());
        Assert.Equal(payment.Status.ToString(), result.GetProperty("Status").GetString());
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-10")]
    public async Task NonPositiveAmountsAreInvalidAndDoNotCreatePayments(string value)
    {
        var booking = Guid.NewGuid();
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        var error = await Assert.ThrowsAsync<ArgumentException>(() =>
            Service.PayAsync(booking, amount, Guid.NewGuid()));
        Assert.Equal("amount", error.ParamName);
        Assert.Empty(_repository.GetPaymentByBookingId(booking));
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
    [InlineData(PaymentStatus.APPROVED)]
    [InlineData(PaymentStatus.REJECTED)]
    public async Task RepeatingAttemptReturnsOriginalPayment(PaymentStatus status)
    {
        const decimal amount = 10;
        var service = Service;
        var first = CreateStoredPayment(status);
        var booking = first.BookingId;
        var attempt = first.PaymentAttemptId;
        var repeated = await service.PayAsync(booking, amount, attempt);
        Assert.Equal(first.PaymentId, repeated.PaymentId);
        Assert.Equal(first.Status, repeated.Status);
        Assert.Single(_repository.GetPaymentByBookingId(booking));
        var events = _publisher.Messages.ToArray();
        Assert.Equal(new[] { "InitProcess", "EndProcess" },
            events.Select(e => e.Action));
        Assert.Equal(events[0].CorrelationId, events[1].CorrelationId);
        await Assert.ThrowsAsync<PaymentConflictException>(() => service.PayAsync(booking, amount + 1, attempt));
        Assert.Equal("EmitError", _publisher.Messages.Last().Action);
    }

    [Fact]
    public async Task NewAttemptCanFollowRejection()
    {
        var service = Service;
        var rejected = CreateStoredPayment(PaymentStatus.REJECTED);
        var booking = rejected.BookingId;
        var next = await service.PayAsync(booking, 10, Guid.NewGuid());
        Assert.NotEqual(rejected.PaymentId, next.PaymentId);
        Assert.Contains(next.Status, new[] { PaymentStatus.APPROVED, PaymentStatus.REJECTED });
        Assert.Equal(2, _repository.GetPaymentByBookingId(booking).Count);
    }

    [Fact]
    public async Task ApprovalBlocksNewAttempts()
    {
        var approved = CreateStoredPayment(PaymentStatus.APPROVED);
        await Assert.ThrowsAsync<PaymentConflictException>(() =>
            Service.PayAsync(approved.BookingId, 10, Guid.NewGuid()));
        Assert.Single(_repository.GetPaymentByBookingId(approved.BookingId));
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
        Assert.Contains(stored.Status, new[] { PaymentStatus.APPROVED, PaymentStatus.REJECTED });
        Assert.All(payments, p => Assert.Contains(p.Status,
            new[] { PaymentStatus.PROCESSING, stored.Status }));
    }

    [Fact]
    public async Task ConcurrentDifferentAttemptsAllowRejectionsButAtMostOneApproval()
    {
        var booking = Guid.NewGuid();
        var results = await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => Task.Run(async () =>
        {
            using var db = _database.CreateDbContext();
            var service = CreateService(new PaymentRepository(db));
            try { return (PaymentEntity?)await service.PayAsync(booking, 10, Guid.NewGuid()); }
            catch (PaymentConflictException) { return null; }
        })));
        var completed = results.OfType<PaymentEntity>().ToArray();
        var stored = _repository.GetPaymentByBookingId(booking);
        Assert.NotEmpty(completed);
        Assert.Equal(completed.Select(p => p.PaymentId).OrderBy(id => id),
            stored.Select(p => p.PaymentId).OrderBy(id => id));
        Assert.All(stored, p => Assert.Contains(p.Status,
            new[] { PaymentStatus.APPROVED, PaymentStatus.REJECTED }));
        Assert.InRange(stored.Count(p => p.Status == PaymentStatus.APPROVED), 0, 1);
    }

    private PaymentEntity CreateStoredPayment(PaymentStatus status)
    {
        var payment = new PaymentEntity
        {
            BookingId = Guid.NewGuid(),
            PaymentAttemptId = Guid.NewGuid(),
            Amount = 10
        };
        _repository.CreatePayment(payment);
        return _repository.UpdatePaymentStatus(payment.PaymentId, status)!;
    }

    [Theory]
    [InlineData(10)]
    [InlineData(1)]
    public async Task BrokerFailureDoesNotPreventPayment(int amount)
    {
        var service = new PaymentService(_repository, new PaymentAuditPublisher(
            new TestMessagePublisher { FailPublishing = true }, NullLogger<PaymentAuditPublisher>.Instance),
            new ConfigurationBuilder().Build());

        var payment = await service.PayAsync(Guid.NewGuid(), amount, Guid.NewGuid());

        Assert.Contains(payment.Status, new[] { PaymentStatus.APPROVED, PaymentStatus.REJECTED });
        Assert.Equal(payment.Status, _repository.GetPayment(payment.PaymentId)!.Status);
    }

    [Fact]
    public async Task BrokerFailureDoesNotReplaceOriginalPaymentError()
    {
        var service = new PaymentService(_repository, new PaymentAuditPublisher(
            new TestMessagePublisher { FailPublishing = true }, NullLogger<PaymentAuditPublisher>.Instance),
            new ConfigurationBuilder().Build());

        var error = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.PayAsync(Guid.Empty, 10, Guid.NewGuid()));

        Assert.Equal("bookingId", error.ParamName);
    }

    public void Dispose() => _db.Dispose();
}
