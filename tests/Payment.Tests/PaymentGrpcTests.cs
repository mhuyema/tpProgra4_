using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Payment.Contracts;
using ProtoBuf.Grpc.Client;
using Xunit;

namespace Payment.Tests;

public sealed class PaymentGrpcTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly IPaymentGrpcService _client;

    public PaymentGrpcTests(WebApplicationFactory<Program> factory)
    {
        _channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        { HttpHandler = factory.Server.CreateHandler() });
        _client = _channel.CreateGrpcService<IPaymentGrpcService>();
    }

    [Theory]
    [InlineData("123.4567", PaymentStatus.APPROVED)]
    [InlineData("0", PaymentStatus.REJECTED)]
    [InlineData("-0.01", PaymentStatus.REJECTED)]
    public async Task GrpcClientRoundTripsAndReusesAttempt(string value, PaymentStatus status)
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
        Assert.Equal(status, first.Status);
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

    [Fact]
    public async Task ConflictsReturnAlreadyExists()
    {
        var request = new PayRequest { BookingId = Guid.NewGuid(), PaymentAttemptId = Guid.NewGuid(), Amount = 10 };
        await _client.PayAsync(request);
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
        Assert.Equal(PaymentStatus.APPROVED, final.Status);
    }

    public void Dispose() => _channel.Dispose();
}
