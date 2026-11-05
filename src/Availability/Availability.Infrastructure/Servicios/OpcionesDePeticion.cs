namespace Availability.Infrastructure.Servicios
{
    /// <summary>
    /// "Etiquetas" que se le pueden pegar a un pedido HTTP para que los handlers del HttpClient sepan qué hacer con él.
    ///
    /// Availability llama a Booking de DOS formas distintas:
    ///   - Con el token del USUARIO logueado (ver el estado de SU reserva).
    ///   - Con el token del SERVICIO Availability, sin usuario (pantalla pública de asientos ocupados).
    /// Esta etiqueta le dice a los handlers cuál de las dos corresponde en cada pedido.
    /// </summary>
    public static class OpcionesDePeticion
    {
        // HttpRequestOptionsKey es la forma oficial de adjuntar datos a un HttpRequestMessage.
        public static readonly HttpRequestOptionsKey<bool> UsarTokenDeServicio = new("Availability.UsarTokenDeServicio");
    }
}
