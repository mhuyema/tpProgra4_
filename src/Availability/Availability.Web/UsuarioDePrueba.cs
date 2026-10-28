namespace Availability.Web;

/// <summary>
/// Usuario fijo mientras no existan las cuentas. Es el único lugar a reemplazar
/// por el usuario autenticado cuando se implemente el login.
/// </summary>
public static class UsuarioDePrueba
{
    public const string Id = "usuario-de-prueba";
    public const string Nombre = "Usuario de Prueba";
    public const string Email = "prueba@bombo.test";
}
