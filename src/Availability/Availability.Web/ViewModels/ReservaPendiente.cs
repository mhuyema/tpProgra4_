namespace Availability.Web.Models;

/// <summary>
/// Selección de asientos guardada en Session mientras la persona inicia sesión,
/// para restaurarla al volver a la pantalla de asientos.
/// </summary>
public record ReservaPendiente(int EventoId, List<int> AsientoIds);
