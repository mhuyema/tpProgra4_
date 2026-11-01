using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace Authorization.Server;

public sealed class AuthorizationServerOptions
{
    public const string SectionName = "Authorization";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Required]
    public string ClientRedirectUri { get; set; } = string.Empty;

    [Required]
    public string GoogleClientId { get; set; } = string.Empty;

    [Required]
    public string GoogleClientSecret { get; set; } = string.Empty;

    [Required]
    public string GoogleRedirectUri { get; set; } = string.Empty;

    public string GoogleCallbackPath { get; set; } = "/signin-google";

    public string? SigningCertificatePath { get; set; }
    public string? SigningCertificatePassword { get; set; }
    public string? EncryptionCertificatePath { get; set; }
    public string? EncryptionCertificatePassword { get; set; }

    public static AuthorizationServerOptions Load(IConfiguration configuration, IHostEnvironment environment)
    {
        var options = configuration.GetSection(SectionName).Get<AuthorizationServerOptions>() ?? new();
        var errors = new List<string>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            errors.Add("ConnectionStrings:DefaultConnection es obligatorio.");

        if (string.IsNullOrWhiteSpace(options.Issuer) || !Uri.TryCreate(options.Issuer, UriKind.Absolute, out var issuer))
            errors.Add("Authorization:Issuer debe ser una URI absoluta.");
        else if (environment.IsProduction() && issuer.Scheme != Uri.UriSchemeHttps)
            errors.Add("Authorization:Issuer debe usar HTTPS en Production.");

        if (string.IsNullOrWhiteSpace(options.Audience))
            errors.Add("Authorization:Audience es obligatorio.");

        ValidateUri(options.ClientRedirectUri, "Authorization:ClientRedirectUri", environment, errors);
        ValidateUri(options.GoogleRedirectUri, "Authorization:GoogleRedirectUri", environment, errors);

        if (string.IsNullOrWhiteSpace(options.GoogleClientId))
            errors.Add("Authorization:GoogleClientId es obligatorio.");
        if (string.IsNullOrWhiteSpace(options.GoogleClientSecret))
            errors.Add("Authorization:GoogleClientSecret es obligatorio.");
        if (string.IsNullOrWhiteSpace(options.GoogleCallbackPath) || !options.GoogleCallbackPath.StartsWith('/'))
            errors.Add("Authorization:GoogleCallbackPath debe ser una ruta que empiece por '/'.");

        if (environment.IsProduction())
        {
            ValidateCertificate(options.SigningCertificatePath, options.SigningCertificatePassword, "Signing", errors);
            ValidateCertificate(options.EncryptionCertificatePath, options.EncryptionCertificatePassword, "Encryption", errors);
        }

        if (errors.Count > 0)
            throw new InvalidOperationException("La configuración del Authorization Server es inválida:\n- " + string.Join("\n- ", errors));

        return options;
    }

    private static void ValidateUri(string? value, string name, IHostEnvironment environment, ICollection<string> errors)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            errors.Add($"{name} debe ser una URI absoluta.");
        else if (environment.IsProduction() && uri.Scheme != Uri.UriSchemeHttps)
            errors.Add($"{name} debe usar HTTPS en Production.");
    }

    private static void ValidateCertificate(string? path, string? password, string name, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            errors.Add($"Authorization:{name}CertificatePath es obligatorio en Production.");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
            errors.Add($"Authorization:{name}CertificatePassword es obligatorio en Production.");

        if (!File.Exists(path))
        {
            errors.Add($"No se encontró el certificado {name} en '{path}'.");
            return;
        }

        try
        {
            using var certificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(path, password);
            if (!certificate.HasPrivateKey)
                errors.Add($"El certificado {name} debe contener una clave privada.");
        }
        catch (Exception exception) when (exception is CryptographicException or UnauthorizedAccessException)
        {
            errors.Add($"No se pudo cargar el certificado {name}: {exception.Message}");
        }
    }
}
