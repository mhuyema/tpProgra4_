using Availability.Web.Models.Components;

namespace Availability.Web.Models;

public record SeatSelectionViewModel(
    int EventoId,
    EventSummaryModel Event,
    SeatMapModel Map,
    bool Autenticado = false,
    IReadOnlyList<int>? Preseleccion = null,
    string? LoginError = null,
    string? LoginEmail = null);
