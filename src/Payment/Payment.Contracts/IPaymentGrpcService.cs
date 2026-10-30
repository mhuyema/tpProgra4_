using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace Payment.Contracts;

[Service]
public interface IPaymentGrpcService
{
    [Operation]
    Task<PayResponse> PayAsync(PayRequest request, CallContext context = default);
}
