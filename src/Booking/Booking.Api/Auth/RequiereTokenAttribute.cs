using Microsoft.AspNetCore.Mvc;

namespace Booking.API.Auth;

// ---------------------------------------------------------------------------------------------
// ¿Quién puede llamar a cada endpoint de Booking?
// El TP (punto 3.e) pide que los microservicios se autentiquen entre sí con el servidor de autorización.
// Antes los GET de ReservasController estaban abiertos: cualquiera que conociera la URL podía consultarlos.
// Ahora cada endpoint declara qué tipo de llamador acepta con un atributo, por ejemplo:
//
//     [RequiereToken(TipoLlamador.Usuario)]
//     [HttpGet("solicitudes/{requestId:guid}")]
//
// Un "atributo" en C# es una etiqueta que se pone arriba de un método o una clase. Esta etiqueta le dice
// a ASP.NET: "antes de ejecutar este método, pasá por AutenticacionRestFilter".
// ---------------------------------------------------------------------------------------------

/// <summary>Tipos de llamador que Booking sabe reconocer.</summary>
public enum TipoLlamador
{
    /// <summary>Una persona logueada. El token es el de su sesión (el sub es el id del usuario).</summary>
    Usuario,

    /// <summary>
    /// El microservicio Availability actuando por su cuenta (sin usuario). El token es de client credentials
    /// y su sub es el client_id "availability-service".
    /// </summary>
    ServicioAvailability
}

// TypeFilterAttribute permite que el filtro reciba cosas por inyección de dependencias (el validador, la
// configuración, el logger) y a la vez un dato fijo que le pasamos nosotros en el atributo (el tipo de llamador).
public class RequiereTokenAttribute : TypeFilterAttribute
{
    public RequiereTokenAttribute(TipoLlamador tipo) : base(typeof(AutenticacionRestFilter))
    {
        // Estos "Arguments" son los que van al constructor del filtro además de lo que inyecta .NET.
        Arguments = new object[] { tipo };
    }
}