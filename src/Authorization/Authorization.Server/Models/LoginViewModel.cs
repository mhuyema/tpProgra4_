namespace Authorization.Server.Models;

public sealed class LoginViewModel
{
    public string? Email { get; set; }
    public string? ReturnUrl { get; set; }
    public string? Error { get; set; }
}
