using Payment.Business.Exceptions;
using Payment.Data.Entities;
using Payment.Data.Exceptions;
using Payment.Data.Repositories;
using PaymentEntity = Payment.Data.Entities.Payment;
using Broker.Messaging.Abstractions;
using Broker.Messaging.Contracts;
namespace Payment.Business.Services;

public sealed class PaymentService(
    IPaymentRepository repository,
    IMessagePublisher publisher) : IPaymentService
{
    public async Task<PaymentEntity> PayAsync(Guid bookingId, decimal amount, Guid paymentAttemptId)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentException("BookingId no puede ser vacío.", nameof(bookingId));
        if (paymentAttemptId == Guid.Empty)
            throw new ArgumentException("PaymentAttemptId no puede ser vacío.", nameof(paymentAttemptId));

        var existing = repository.GetPaymentByAttempt(bookingId, paymentAttemptId);
        if (existing is not null)
            return ReusePayment(existing, amount);

        if (repository.GetPaymentByBookingId(bookingId).Any(p =>
            p.Status is PaymentStatus.APPROVED or PaymentStatus.PROCESSING))
        {
            // El mismo intento pudo insertarse entre las dos consultas.
            existing = repository.GetPaymentByAttempt(bookingId, paymentAttemptId);
            if (existing is not null)
                return ReusePayment(existing, amount);

            throw new PaymentConflictException("La reserva ya tiene un pago APPROVED o PROCESSING.");
        }

        var payment = new PaymentEntity
        {
            BookingId = bookingId,
            PaymentAttemptId = paymentAttemptId,
            Amount = amount,
            Status = PaymentStatus.PROCESSING
        };

        try
        {
            repository.CreatePayment(payment);
        }
        catch (DuplicatePaymentException)
        {
            // Otra solicitud ganó la inserción: consultar lo que guardó en SQL.
            existing = repository.GetPaymentByAttempt(bookingId, paymentAttemptId);
            if (existing is not null)
                return ReusePayment(existing, amount);

            throw new PaymentConflictException("La reserva ya tiene un pago APPROVED o PROCESSING.");
        }

        // Simula una operación asíncrona; los reintentos pueden consultar PROCESSING.
        // Desconectar el cliente no cancela un cobro ya creado.
        await Task.Yield();
        var finalStatus = amount > 0 ? PaymentStatus.APPROVED : PaymentStatus.REJECTED;
        return repository.UpdatePaymentStatus(payment.PaymentId, finalStatus)
            ?? throw new InvalidOperationException("El pago creado no se encuentra.");
    }

    private static PaymentEntity ReusePayment(PaymentEntity existing, decimal amount)
    {
        if (existing.Amount != amount)
            throw new PaymentConflictException("El intento ya existe con un importe distinto.");

        return existing;
    }
}
