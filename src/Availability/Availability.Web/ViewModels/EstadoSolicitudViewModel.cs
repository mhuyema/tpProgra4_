using Availability.Web.Models.Components;

namespace Availability.Web.Models;

public record SolicitudEstadoItem(Guid SolicitudId, string Estado, string? Motivo, decimal Monto);

// Un aviso del servicio de notificaciones listo para mostrar (punto 3.h)
public record NotificacionItem(string Tipo, string Mensaje, DateTime EnviadaUtc);

public record EstadoSolicitudViewModel(
    int EventoId,
    EventSummaryModel Event,
    IReadOnlyList<SolicitudEstadoItem> Solicitudes,
    // Parametro opcional, si Notificaciones no responde, la pantalla se muetra igual, sin avisos de notificaciones (punto 3.h)
    IReadOnlyList<NotificacionItem>? Notificaciones = null);
