using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Notifications.Api.Services;

// Envio real por SMTP. Implementa el mismo puerto que SimulatedNotificationSender, asi los handlers no cambian:
// Program.cs elige cual usar segun haya o no un servidor SMTP configurado.
public sealed class EmailNotificationSender : INotificationSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<EmailNotificationSender> _logger;

    public EmailNotificationSender(IOptions<SmtpOptions> options, ILogger<EmailNotificationSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(Notification notification)
    {
        var mail = new MimeMessage();
        mail.From.Add(MailboxAddress.Parse(_options.From));
        mail.To.Add(MailboxAddress.Parse(_options.To));
        mail.Subject = notification.Type == "ReservaConfirmada"
            ? "Tu reserva esta confirmada"
            : "Recibimos tu reserva";
        mail.Body = new TextPart("plain") { Text = notification.Message };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_options.Host, _options.Port,
            _options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
        if (!string.IsNullOrEmpty(_options.User))
            await smtp.AuthenticateAsync(_options.User, _options.Password ?? "");
        await smtp.SendAsync(mail);
        await smtp.DisconnectAsync(true);

        _logger.LogInformation(
            "[AUDITORIA] MAIL enviado. correlationId={RequestId} tipo={Tipo} usuario={UserId} a={Email}",
            notification.RequestId, notification.Type, notification.UserId, _options.To);
    }
}
