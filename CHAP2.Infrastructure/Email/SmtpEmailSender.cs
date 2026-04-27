using System.Net;
using System.Net.Mail;
using CHAP2.Infrastructure.Identity;
using CHAP2.Shared.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CHAP2.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender<ApplicationUser>
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        var url = !string.IsNullOrWhiteSpace(_settings.EmailConfirmationUrlTemplate)
            ? _settings.EmailConfirmationUrlTemplate.Replace("{token}", Uri.EscapeDataString(confirmationLink), StringComparison.Ordinal)
                                                   .Replace("{userId}", Uri.EscapeDataString(user.Id), StringComparison.Ordinal)
            : confirmationLink;

        return SendAsync(email, "Confirm your CHAP2 account",
            $"Hi {user.UserName},\n\nClick the link below to confirm your account:\n{url}");
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        var url = !string.IsNullOrWhiteSpace(_settings.PasswordResetUrlTemplate)
            ? _settings.PasswordResetUrlTemplate.Replace("{token}", Uri.EscapeDataString(resetLink), StringComparison.Ordinal)
                                                .Replace("{userId}", Uri.EscapeDataString(user.Id), StringComparison.Ordinal)
                                                .Replace("{email}", Uri.EscapeDataString(email), StringComparison.Ordinal)
            : resetLink;

        return SendAsync(email, "Reset your CHAP2 password",
            $"Hi {user.UserName},\n\nClick the link below to reset your password:\n{url}\n\nIf you didn't request this, you can ignore this email.");
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        SendAsync(email, "CHAP2 password reset code",
            $"Hi {user.UserName},\n\nUse this code to reset your password:\n{resetCode}");

    private async Task SendAsync(string to, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host))
        {
            _logger.LogWarning("SMTP host not configured; email to {To} with subject '{Subject}' was dropped.", to, subject);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromAddress, _settings.FromDisplayName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false,
        };
        message.To.Add(to);

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.UseStartTls,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_settings.UserName, _settings.Password),
        };

        await client.SendMailAsync(message);
        _logger.LogInformation("Sent email to {To} with subject '{Subject}'", to, subject);
    }
}
