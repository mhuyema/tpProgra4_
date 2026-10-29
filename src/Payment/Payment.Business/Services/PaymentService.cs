using Payment.Business.Exceptions;
using Payment.Data.Entities;
using Payment.Data.Repositories;
using PaymentEntity = Payment.Data.Entities.Payment;

namespace Payment.Business.Services;

// Singleton: todas las llamadas comparten la exclusión de validación y creación.
public sealed class PaymentService(IPaymentRepository repository) : IPaymentService
{
    private readonly object _creationLock = new();

    public async Task<PaymentEntity> PayAsync(Guid bookingId, decimal amount, Guid paymentAttemptId)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentException("BookingId no puede ser vacío.", nameof(bookingId));
        if (paymentAttemptId == Guid.Empty)
            throw new ArgumentException("PaymentAttemptId no puede ser vacío.", nameof(paymentAttemptId));

        PaymentEntity payment;
        lock (_creationLock)
        {
            var existing = repository.GetPaymentByAttempt(bookingId, paymentAttemptId);
            if (existing is not null)
            {
                if (existing.Amount != amount)
                    throw new PaymentConflictException("El intento ya existe con un importe distinto.");
                return existing;
            }

            if (repository.GetPaymentByBookingId(bookingId).Any(p =>
                p.Status is PaymentStatus.APPROVED or PaymentStatus.PROCESSING))
                throw new PaymentConflictException("La reserva ya tiene un pago APPROVED o PROCESSING.");

            payment = new PaymentEntity
            {
                BookingId = bookingId,
                PaymentAttemptId = paymentAttemptId,
                Amount = amount,
                Status = PaymentStatus.PROCESSING
            };
            repository.CreatePayment(payment);
        }

        // Simula una operación asíncrona; los reintentos pueden consultar PROCESSING.
        // Desconectar el cliente no cancela un cobro ya creado.
        await Task.Yield();
        var finalStatus = amount > 0 ? PaymentStatus.APPROVED : PaymentStatus.REJECTED;
        return repository.UpdatePaymentStatus(payment.PaymentId, finalStatus)
            ?? throw new InvalidOperationException("El pago creado no se encuentra.");
    }
}
