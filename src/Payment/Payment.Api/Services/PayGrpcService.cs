using Grpc.Core;
using Payment.Business.Exceptions;
using Payment.Business.Services;
using Payment.Contracts;
using ProtoBuf.Grpc;
using DomainStatus = Payment.Data.Entities.PaymentStatus;

namespace Payment.Api.Services;

public sealed class PayGrpcService(IPaymentService paymentService) : IPaymentGrpcService
{
    public async Task<PayResponse> PayAsync(PayRequest request, CallContext context = default)
    {
        try
        {
            var payment = await paymentService.PayAsync(request.BookingId, request.Amount, request.PaymentAttemptId);
            return new PayResponse
            {
                BookingId = payment.BookingId,
                Amount = payment.Amount,
                PaymentId = payment.PaymentId,
                Status = payment.Status switch
                {
                    DomainStatus.PROCESSING => PaymentStatus.PROCESSING,
                    DomainStatus.APPROVED => PaymentStatus.APPROVED,
                    DomainStatus.REJECTED => PaymentStatus.REJECTED,
                    _ => throw new InvalidOperationException("Estado de pago desconocido.")
                }
            };
        }
        catch (ArgumentException exception)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, exception.Message));
        }
        catch (PaymentConflictException exception)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, exception.Message));
        }
    }
}
