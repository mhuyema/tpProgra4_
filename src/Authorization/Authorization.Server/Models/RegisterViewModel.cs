namespace Authorization.Server.Models;

public sealed class RegisterViewModel
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? ReturnUrl { get; set; }
    public string? Error { get; set; }
}
