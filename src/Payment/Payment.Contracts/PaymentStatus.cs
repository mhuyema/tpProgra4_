using ProtoBuf;

namespace Payment.Contracts;

[ProtoContract]
public enum PaymentStatus
{
    [ProtoEnum] UNSPECIFIED = 0,
    [ProtoEnum] PROCESSING = 1,
    [ProtoEnum] APPROVED = 2,
    [ProtoEnum] REJECTED = 3
}
