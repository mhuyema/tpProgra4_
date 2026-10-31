namespace Payment.Data.Exceptions;

// La capa Business puede resolver la carrera sin depender de EF Core ni SqlClient.
public sealed class DuplicatePaymentException(Exception innerException)
    : InvalidOperationException("El pago, intento o un pago activo de la reserva ya existe.", innerException);
