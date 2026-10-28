using Availability.Web.Models.Components;

namespace Availability.Web.Models;

public record SeatSelectionViewModel(Guid EventoId, EventSummaryModel Event, SeatMapModel Map);
