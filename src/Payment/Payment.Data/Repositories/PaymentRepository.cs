using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Payment.Data.Entities;
using Payment.Data.Exceptions;
using PaymentEntity = Payment.Data.Entities.Payment;

namespace Payment.Data.Repositories;

public sealed class PaymentRepository(AppDbContext db) : IPaymentRepository
{
    public void CreatePayment(PaymentEntity payment)
    {
        if (payment.Status != PaymentStatus.PROCESSING)
            throw new InvalidOperationException("Un pago nuevo debe estar PROCESSING.");

        db.Payments.Add(payment);
        try
        {
            db.SaveChanges();
        }
        // Los índices únicos coordinan también solicitudes de distintas instancias.
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new DuplicatePaymentException(exception);
        }
        finally
        {
            // Descarta la inserción fallida e impide cambios externos sin guardar.
            db.Entry(payment).State = EntityState.Detached;
        }
    }

    public PaymentEntity? GetPayment(Guid paymentId) =>
        db.Payments.AsNoTracking().FirstOrDefault(p => p.PaymentId == paymentId);

    public IReadOnlyList<PaymentEntity> GetPaymentByBookingId(Guid bookingId) =>
        db.Payments.AsNoTracking().Where(p => p.BookingId == bookingId).ToArray();

    public PaymentEntity? GetPaymentByAttempt(Guid bookingId, Guid paymentAttemptId) =>
        db.Payments.AsNoTracking().FirstOrDefault(p =>
            p.BookingId == bookingId && p.PaymentAttemptId == paymentAttemptId);

    public PaymentEntity? UpdatePaymentStatus(Guid paymentId, PaymentStatus status)
    {
        if (status is PaymentStatus.APPROVED or PaymentStatus.REJECTED)
        {
            // La condición en SQL impide que dos solicitudes finalicen el mismo pago.
            var updated = db.Payments
                .Where(p => p.PaymentId == paymentId && p.Status == PaymentStatus.PROCESSING)
                .ExecuteUpdate(setters => setters.SetProperty(p => p.Status, status));

            if (updated == 1)
                return GetPayment(paymentId);
        }

        if (GetPayment(paymentId) is null)
            return null;

        throw new InvalidOperationException("Solo se permite pasar de PROCESSING a APPROVED o REJECTED.");
    }
}
