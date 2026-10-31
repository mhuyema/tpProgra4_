using Payment.Business.Exceptions;
using Payment.Data.Entities;
using Payment.Data.Exceptions;
using Payment.Data.Repositories;
using PaymentEntity = Payment.Data.Entities.Payment;
using Microsoft.Extensions.Configuration;
namespace Payment.Business.Services;

public sealed class PaymentService(
    IPaymentRepository repository,
    IConfiguration configuration
    ) : IPaymentService
{
    private readonly int approvalRandomPercentage = configuration.GetValue<int>("Payment:ApprovalRandomPercentage",20);
    public async Task<PaymentEntity> PayAsync(Guid bookingId, decimal amount, Guid paymentAttemptId)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentException("BookingId no puede ser vacío.", nameof(bookingId));
        if (paymentAttemptId == Guid.Empty)
            throw new ArgumentException("PaymentAttemptId no puede ser vacío.", nameof(paymentAttemptId));
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than 0", nameof(amount));

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
        var finalStatus = Random.Shared.Next(100) < 50 ? PaymentStatus.REJECTED : PaymentStatus.APPROVED;
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
