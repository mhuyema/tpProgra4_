using Availability.Web.Models.Components;

namespace Availability.Web.Models;

public record SolicitudEstadoItem(Guid SolicitudId, string Estado, string? Motivo, decimal Monto);

public record EstadoSolicitudViewModel(
    int EventoId,
    EventSummaryModel Event,
    IReadOnlyList<SolicitudEstadoItem> Solicitudes);
