using Grpc.Core;
using Grpc.Net.Client;
using Payment.Contracts;
using Payment.Data.Repositories;
using ProtoBuf.Grpc.Client;
using Xunit;
using DomainStatus = Payment.Data.Entities.PaymentStatus;
using PaymentEntity = Payment.Data.Entities.Payment;

namespace Payment.Tests;

[Collection(PaymentDatabaseCollection.Name)]
public sealed class PaymentGrpcTests : IDisposable
{
    private readonly PaymentWebApplicationFactory _factory;
    private readonly PaymentDatabaseFixture _database;
    private readonly GrpcChannel _channel;
    private readonly IPaymentGrpcService _client;

    public PaymentGrpcTests(PaymentDatabaseFixture database)
    {
        _database = database;
        _factory = new PaymentWebApplicationFactory(database.ConnectionString);
        _channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        { HttpHandler = _factory.Server.CreateHandler() });
        _client = _channel.CreateGrpcService<IPaymentGrpcService>();
    }

    [Theory]
    [InlineData("123.45")]
    [InlineData("0.01")]
    public async Task GrpcClientRoundTripsAndReusesAttempt(string value)
    {
        var booking = Guid.NewGuid();
        var attempt = Guid.NewGuid();
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        var request = new PayRequest { BookingId = booking, Amount = amount, PaymentAttemptId = attempt };
        var first = await _client.PayAsync(request);
        var repeated = await _client.PayAsync(request);
        Assert.Equal(booking, first.BookingId);
        Assert.Equal(amount, first.Amount);
        Assert.NotEqual(Guid.Empty, first.PaymentId);
        Assert.Contains(first.Status, new[] { PaymentStatus.APPROVED, PaymentStatus.REJECTED });
        Assert.Equal(first.PaymentId, repeated.PaymentId);
        Assert.Equal(first.Status, repeated.Status);
        Assert.Equal(first.Amount, repeated.Amount);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-10")]
    public async Task NonPositiveAmountsReturnInvalidArgumentWithoutCreatingPayment(string value)
    {
        var booking = Guid.NewGuid();
        var error = await Assert.ThrowsAsync<RpcException>(() => _client.PayAsync(new PayRequest
        {
            BookingId = booking,
            PaymentAttemptId = Guid.NewGuid(),
            Amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture)
        }));
        Assert.Equal(StatusCode.InvalidArgument, error.StatusCode);
        using var db = _database.CreateDbContext();
        Assert.Empty(new PaymentRepository(db).GetPaymentByBookingId(booking));
    }

    [Theory]
    [InlineData(DomainStatus.APPROVED, PaymentStatus.APPROVED)]
    [InlineData(DomainStatus.REJECTED, PaymentStatus.REJECTED)]
    public async Task GrpcRetriesPreserveStoredFinalState(DomainStatus status, PaymentStatus expected)
    {
        var request = CreateStoredPayment(status);
        var first = await _client.PayAsync(request);
        var repeated = await _client.PayAsync(request);
        Assert.Equal(expected, first.Status);
        Assert.Equal(expected, repeated.Status);
        Assert.Equal(first.PaymentId, repeated.PaymentId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InvalidIdsReturnInvalidArgument(bool emptyBooking)
    {
        var error = await Assert.ThrowsAsync<RpcException>(() => _client.PayAsync(new PayRequest
        {
            BookingId = emptyBooking ? Guid.Empty : Guid.NewGuid(),
            PaymentAttemptId = emptyBooking ? Guid.NewGuid() : Guid.Empty,
            Amount = 10
        }));
        Assert.Equal(StatusCode.InvalidArgument, error.StatusCode);
    }

    [Theory]
    [InlineData(DomainStatus.APPROVED)]
    [InlineData(DomainStatus.PROCESSING)]
    public async Task ConflictsReturnAlreadyExists(DomainStatus status)
    {
        var request = CreateStoredPayment(status);
        request.Amount = 11;
        var error = await Assert.ThrowsAsync<RpcException>(() => _client.PayAsync(request));
        Assert.Equal(StatusCode.AlreadyExists, error.StatusCode);
        request.PaymentAttemptId = Guid.NewGuid();
        error = await Assert.ThrowsAsync<RpcException>(() => _client.PayAsync(request));
        Assert.Equal(StatusCode.AlreadyExists, error.StatusCode);
    }

    [Fact]
    public async Task ConcurrentGrpcRetriesReturnOnePaymentAndEventuallyItsFinalState()
    {
        var request = new PayRequest
        {
            BookingId = Guid.NewGuid(),
            PaymentAttemptId = Guid.NewGuid(),
            Amount = 150.75m
        };
        var responses = await Task.WhenAll(Enumerable.Range(0, 50).Select(_ => _client.PayAsync(request)));
        var paymentId = responses[0].PaymentId;
        Assert.All(responses, response => Assert.Equal(paymentId, response.PaymentId));
        var final = await _client.PayAsync(request);
        Assert.Equal(paymentId, final.PaymentId);
        Assert.Contains(final.Status, new[] { PaymentStatus.APPROVED, PaymentStatus.REJECTED });
        Assert.All(responses, response => Assert.Contains(response.Status,
            new[] { PaymentStatus.PROCESSING, final.Status }));
    }

    private PayRequest CreateStoredPayment(DomainStatus status)
    {
        using var db = _database.CreateDbContext();
        var repository = new PaymentRepository(db);
        var payment = new PaymentEntity
        {
            BookingId = Guid.NewGuid(),
            PaymentAttemptId = Guid.NewGuid(),
            Amount = 10
        };
        repository.CreatePayment(payment);
        if (status != DomainStatus.PROCESSING)
            repository.UpdatePaymentStatus(payment.PaymentId, status);
        return new PayRequest
        {
            BookingId = payment.BookingId,
            PaymentAttemptId = payment.PaymentAttemptId,
            Amount = payment.Amount
        };
    }

    public void Dispose()
    {
        _channel.Dispose();
        _factory.Dispose();
    }
}
