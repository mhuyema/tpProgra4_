namespace Payment.Business.Exceptions;

public sealed class PaymentConflictException(string message) : Exception(message);
