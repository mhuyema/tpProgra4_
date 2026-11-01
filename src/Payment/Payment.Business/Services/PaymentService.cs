using Payment.Business.Exceptions;
using Payment.Data.Entities;
using Payment.Data.Exceptions;
using Payment.Data.Repositories;
using PaymentEntity = Payment.Data.Entities.Payment;
using Payment.Business.Auditing;
namespace Payment.Business.Services;

public sealed class PaymentService(
    IPaymentRepository repository,
    IPaymentAuditPublisher audit) : IPaymentService
{
    public async Task<PaymentEntity> PayAsync(Guid bookingId, decimal amount, Guid paymentAttemptId)
    {
        var correlationId = Guid.NewGuid().ToString();
        const string processName = "ProcesarPago";
        await audit.InitProcessAsync(correlationId, processName);

        try
        {
            var payment = await PayCoreAsync(bookingId, amount, paymentAttemptId);
            await audit.EndProcessAsync(correlationId, processName,
                new { payment.PaymentId, payment.BookingId, payment.PaymentAttemptId, payment.Amount,
                    Status = payment.Status.ToString() });
            return payment;
        }
        catch (Exception exception)
        {
            await audit.EmitErrorAsync(correlationId, processName, exception);
            throw;
        }
    }

    private async Task<PaymentEntity> PayCoreAsync(Guid bookingId, decimal amount, Guid paymentAttemptId)
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
