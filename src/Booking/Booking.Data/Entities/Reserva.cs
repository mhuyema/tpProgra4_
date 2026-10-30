namespace Booking.Data.Entities;

// Dominio anemico: La entidad solo guarda estado, sin comportamiento
// Las reglas (retener, rechazar, vencer) viven en Booking.Business
public class Reserva
{
    public Guid Id { get; set; }
    public Guid SolicitudId { get; set; } // id unico que genera Availability
    public int EventoId { get; set; }
    public int AsientoId { get; set; }
    public string UsuarioId { get; set; } = string.Empty;
    public EstadoReserva Estado { get; set; }
    public MotivoRechazo? Motivo { get; set; } // Solo si Estado = Rechazada
    public DateTime CreadaEn { get; set; }
    public DateTime? VenceEn { get; set; } // Fin de la retencion

    // Importe de la reserva recibido desde Availability.
    // Se conserva para procesar el pago y sus posibles reintentos.
    public decimal Importe { get; set; }

    // Identifica el intento de pago asociado a la reserva.
    // Se conserva para reutilizarlo ante reintentos de comunicación con Payment.
    public Guid? IntentoPagoId { get; set; }
}

public enum EstadoReserva { Retenida, Confirmada, Rechazada, Vencida }

public enum  MotivoRechazo { AsientoOcupado, PagoFallido, PagoNoDisponible }
