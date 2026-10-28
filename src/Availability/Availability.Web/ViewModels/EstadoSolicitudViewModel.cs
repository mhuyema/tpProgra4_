using Availability.Web.Models.Components;

namespace Availability.Web.Models;

public record SolicitudEstadoItem(Guid SolicitudId, string Estado, string? Motivo);

public record EstadoSolicitudViewModel(
    Guid EventoId,
    EventSummaryModel Event,
    IReadOnlyList<SolicitudEstadoItem> Solicitudes);
