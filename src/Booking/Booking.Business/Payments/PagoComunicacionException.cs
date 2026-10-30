namespace Booking.Business.Payments;

// Representa un fallo de comunicación con Payment.
// No implica que el pago haya sido rechazado ni que no se haya procesado.
public class PagoComunicacionException : Exception
{
    public PagoComunicacionException(string mensaje, Exception innerException)
        : base(mensaje, innerException)
    {
    }
}