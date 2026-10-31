namespace Availability.Application.DTOs
{
    /// <summary>
    /// Lo que devuelven ListarEventos y ObtenerEvento.
    /// </summary>
    public record EventoDTO(
        int Id,
        string Nombre,
        string? Descripcion,
        string Lugar,
        DateTime FechaInicio,
        DateTime FechaFin,
        string? ImageUrl,
        int AsientosLibres);
}
