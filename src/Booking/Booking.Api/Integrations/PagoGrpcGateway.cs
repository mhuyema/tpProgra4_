using Booking.Business.Payments;
using Payment.Contracts;
using Grpc.Core;
using ProtoBuf.Grpc;

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

        // Los errores transitorios de gRPC se traducen a una excepción interna.
        // Business podrá decidir si corresponde reintentar el mismo pago,
        // sin interpretar una falla de comunicación como un rechazo.
        
        PayResponse response;

        try
        {
            // Se limita cada llamada a 10 segundos para que una falla de Payment
            // no bloquee indefinidamente el procesamiento ni la recuperación.
            var opciones = new CallOptions(
                deadline: DateTime.UtcNow.AddSeconds(10));

            response = await _paymentClient.PayAsync(
                request,
                new CallContext(opciones));
        }
        catch (RpcException ex) when (
            ex.StatusCode == StatusCode.Unavailable ||
            ex.StatusCode == StatusCode.DeadlineExceeded ||
            ex.StatusCode == StatusCode.ResourceExhausted)
        {
            throw new PagoComunicacionException(
                "No se pudo obtener una respuesta de Payment.",
                ex);
        }

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