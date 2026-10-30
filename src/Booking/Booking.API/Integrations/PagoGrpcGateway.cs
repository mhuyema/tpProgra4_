using Booking.Business.Payments;
using Payment.Contracts;

namespace Booking.API.Integrations;

// Adaptador que implementa el contrato interno de Booking
// y realiza la comunicación con Payment mediante gRPC.
public class PagoGrpcGateway : IPagoGateway
{
    private readonly IPaymentGrpcService _paymentClient;

    public PagoGrpcGateway(IPaymentGrpcService paymentClient)
    {
        _paymentClient = paymentClient;
    }

    public async Task<ResultadoPago> ProcesarPagoAsync(
        Guid reservaId,
        decimal importe,
        Guid intentoPagoId)
    {
        // Construye la solicitud utilizando el identificador de intento recibido,
        // sin generar uno nuevo en cada llamada.
        var request = new PayRequest
        {
            BookingId = reservaId,
            Amount = importe,
            PaymentAttemptId = intentoPagoId
        };

        var response = await _paymentClient.PayAsync(request);

        // Traduce explícitamente los estados de Payment a los estados internos
        // de Booking, evitando depender de sus valores numéricos.
        var estado = response.Status switch
        {
            PaymentStatus.PROCESSING => EstadoPago.EnProceso,
            PaymentStatus.APPROVED => EstadoPago.Aprobado,
            PaymentStatus.REJECTED => EstadoPago.Rechazado,

            _ => throw new InvalidOperationException(
                $"Estado de Payment no reconocido: {response.Status}")
        };

        return new ResultadoPago(response.PaymentId, estado);
    }
}