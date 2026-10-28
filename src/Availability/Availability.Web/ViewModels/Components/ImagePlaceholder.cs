namespace Availability.Web.Models.Components;

/// <summary>Imágenes de relleno (data URI) mientras no existan fotos reales.</summary>
public static class ImagePlaceholder
{
    public static string Gradient(string fromHex, string toHex = "131313") =>
        "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='800' height='1000'>" +
        "<defs><linearGradient id='g' x1='0' y1='0' x2='1' y2='1'>" +
        $"<stop offset='0' stop-color='%23{fromHex}'/><stop offset='1' stop-color='%23{toHex}'/>" +
        "</linearGradient></defs><rect width='800' height='1000' fill='url(%23g)'/></svg>";
}
