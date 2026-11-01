using Microsoft.AspNetCore.Identity;

namespace Authorization.Server.Models;

public sealed class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
