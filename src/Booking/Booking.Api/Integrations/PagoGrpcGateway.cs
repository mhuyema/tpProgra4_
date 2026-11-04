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
    private readonly IProveedorTokenServicio _tokens;

    public PagoGrpcGateway(IPaymentGrpcService paymentClient, IProveedorTokenServicio tokens)
    {
        _paymentClient = paymentClient;
        _tokens = tokens;
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
            try
            {
                response = await LlamarAsync(request);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Unauthenticated)
            {
                // El token de servicio pudo vencer: se pide uno nuevo y se reintenta UNA vez.
                // Si Payment sigue rechazándolo es un problema de configuración, no un pago rechazado.
                _tokens.Invalidar();
                response = await LlamarAsync(request);
            }
        }
        catch (HttpRequestException ex)
        {
            // No se pudo obtener el token de servicio (Authorization Server caído): falla de comunicación, no rechazo.
            throw new PagoComunicacionException(
                "No se pudo obtener el token de servicio para llamar a Payment.",
                ex);
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

    // Autenticación servicio a servicio: el token (emitido por el Authorization Server) viaja en la metadata gRPC
    // y Payment lo valida contra el mismo Authorization Server. Nunca se loguea.
    private async Task<PayResponse> LlamarAsync(PayRequest request)
    {
        var token = await _tokens.ObtenerAsync();
        var headers = new Metadata { { "authorization", $"Bearer {token}" } };

        // Se limita cada llamada a 10 segundos para que una falla de Payment
        // no bloquee indefinidamente el procesamiento ni la recuperación.
        var opciones = new CallOptions(
            headers: headers,
            deadline: DateTime.UtcNow.AddSeconds(10));

        return await _paymentClient.PayAsync(request, new CallContext(opciones));
    }
}