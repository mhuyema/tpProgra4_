using Availability.Web.Models.Components;

namespace Availability.Web.Models;

public record SeatSelectionViewModel(int EventoId, EventSummaryModel Event, SeatMapModel Map);
