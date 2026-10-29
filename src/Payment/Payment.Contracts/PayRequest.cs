using ProtoBuf;

namespace Payment.Contracts;

[ProtoContract]
public sealed class PayRequest
{
    [ProtoMember(1)] public Guid BookingId { get; set; }
    [ProtoMember(2)] public decimal Amount { get; set; }
    [ProtoMember(3)] public Guid PaymentAttemptId { get; set; }
}
