using System.Globalization;
using Availability.Application.DTOs;
using Availability.Web.Models.Components;

namespace Availability.Web.Models.Mapping;

/// <summary>
/// Convierte los DTOs de Application en los modelos de los componentes visuales.
/// Mantiene finos a los controllers y concentra el formato (fechas, badges, links).
/// </summary>
public static class ViewModelMapper
{
    private const int UltimosCupos = 20;
    public const string HeroImage = "/imgs/hero.jpg";
    private const string ImagenPorDefecto = "/imgs/hero.jpg";

    private static readonly CultureInfo Es = new("es-AR");

    public static string ReservaHref(Guid eventoId) => $"/Reserva/Asientos/{eventoId}";

    public static EventCardModel ToCard(this EventoDTO e)
    {
        var (badge, isError) = BadgeDe(e);
        return new EventCardModel(
            e.Nombre,
            $"{Fecha(e.FechaInicio)} · {Ciudad(e.Lugar)}",
            e.ImageUrl ?? ImagenPorDefecto,
            ReservaHref(e.Id),
            badge,
            isError);
    }

    public static HeroModel ToHero(this EventoDTO e) => new(
        e.Nombre,
        $"{Fecha(e.FechaInicio)} · {Ciudad(e.Lugar)}",
        e.Descripcion ?? string.Empty,
        HeroImage,
        "Sacar entrada",
        ReservaHref(e.Id),
        "Venta en vivo");

    public static EventSummaryModel ToSummary(this EventoDTO e) => new(
        e.Nombre,
        $"{Fecha(e.FechaInicio)} · {e.FechaInicio:HH:mm} hs",
        e.Lugar,
        e.ImageUrl ?? ImagenPorDefecto,
        BadgeDe(e).Text,
        "Venta en vivo");

    public static SeatMapModel ToSeatMap(this IEnumerable<AsientoDTO> asientos, string stageLabel = "Escenario") =>
        new(stageLabel,
            asientos
                .GroupBy(a => a.Fila)
                .OrderBy(g => g.Key)
                .Select(g => new SeatRowModel(
                    g.Key,
                    g.OrderBy(a => a.Numero)
                     .Select(a => new SeatModel(
                         a.Id.ToString(),
                         $"{a.Fila}{a.Numero}",
                         a.Numero,
                         a.Ocupado ? SeatStatus.Occupied : SeatStatus.Available,
                         a.Precio))
                     .ToList()))
                .ToList());

    private static (string? Text, bool IsError) BadgeDe(EventoDTO e) => e.AsientosLibres switch
    {
        0 => ("Sold out", true),
        <= UltimosCupos => ("Últimos cupos", false),
        _ => (null, false)
    };

    // "Sáb 12 NOV"
    private static string Fecha(DateTime d)
    {
        var dia = Es.DateTimeFormat.GetAbbreviatedDayName(d.DayOfWeek).TrimEnd('.');
        var mes = Es.DateTimeFormat.GetAbbreviatedMonthName(d.Month).TrimEnd('.').ToUpperInvariant();
        return $"{char.ToUpperInvariant(dia[0])}{dia[1..]} {d.Day} {mes}";
    }

    // "Estadio Obras Sanitarias, Buenos Aires" -> "Buenos Aires"
    private static string Ciudad(string lugar) => lugar.Split(',')[^1].Trim();
}
