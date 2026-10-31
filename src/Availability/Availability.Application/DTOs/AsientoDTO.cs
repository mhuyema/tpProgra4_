namespace Availability.Application.DTOs
{
    /// <summary>
    /// Lo que devuelve ConsultarAsientosLibres: todos los asientos del evento, con su estado.
    /// </summary>
    public record AsientoDTO(
        int Id,
        string Fila,
        int Numero,
        decimal Precio,
        bool Ocupado);
}
