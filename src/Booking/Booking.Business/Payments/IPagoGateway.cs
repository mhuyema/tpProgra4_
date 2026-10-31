namespace Booking.Business.Payments;

// Estados de pago utilizados internamente por Booking,
// independientes de los valores del contrato de Payment.
public enum EstadoPago
{
    EnProceso,
    Aprobado,
    Rechazado
}

// Representa el resultado de un pago recibido desde el servicio externo.
public record ResultadoPago(Guid PaymentId, EstadoPago Estado);

// Contrato interno para solicitar pagos desde Booking.Business
// sin depender directamente de gRPC ni de Payment.Contracts.
public interface IPagoGateway
{
    // El identificador del intento debe mantenerse en los reintentos
    // para evitar que Payment procese el mismo pago más de una vez.
    Task<ResultadoPago> ProcesarPagoAsync(
        Guid reservaId,
        decimal importe,
        Guid intentoPagoId
    );
}