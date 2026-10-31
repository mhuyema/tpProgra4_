namespace Availability.Web.Models.Components;

public enum SeatStatus { Available, Occupied }

// Id es el id (int) del asiento (se postea al confirmar); Label es lo que ve el usuario ("A1").
public record SeatModel(string Id, string Label, int Number, SeatStatus Status, decimal Price);

public record SeatRowModel(string Label, IReadOnlyList<SeatModel> Seats);

public record SeatMapModel(string StageLabel, IReadOnlyList<SeatRowModel> Rows, int MaxSeats = 6);

public record EventSummaryModel(
    string Title,
    string Meta,
    string Venue,
    string ImageUrl,
    string? BadgeText = null,
    string? LiveText = null);
