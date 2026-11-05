namespace Notifications.Api.Services;

// Datos del servidor SMTP (seccion "Smtp" de la configuracion). 
// se cargan con User Secrets. Si Host esta vacio, Program.cs sigue usando el envio simulado.
public class SmtpOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 1025;
    public bool UseSsl { get; set; }
    public string? User { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "reservas@grupo9.local";

    // Casilla de demo: todos los avisos llegan aca. El mensaje del broker trae el UserId (sub del token),
    // no el email del usuario.
    public string To { get; set; } = "demo@grupo9.local";
}
