using Availability.Web.Models.Components;

namespace Availability.Web.Models;

public record HomeViewModel(
    HeroModel Hero,
    IReadOnlyList<ChipModel> Filters,
    IReadOnlyList<EventCardModel> Events,
    IReadOnlyList<VenueCardModel> Venues);
