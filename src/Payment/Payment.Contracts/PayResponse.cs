using ProtoBuf;

namespace Payment.Contracts;

[ProtoContract]
public sealed class PayResponse
{
    [ProtoMember(1)] public Guid BookingId { get; set; }
    [ProtoMember(2)] public decimal Amount { get; set; }
    [ProtoMember(3)] public Guid PaymentId { get; set; }
    [ProtoMember(4)] public PaymentStatus Status { get; set; }
}
