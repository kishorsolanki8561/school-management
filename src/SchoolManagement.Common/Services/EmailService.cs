using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SchoolManagement.Common.Configuration;

namespace SchoolManagement.Common.Services;

public sealed class EmailService : IEmailService
{
    private readonly EmailSettings _settings = InitializeConfiguration.EmailSettings;

    public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetUrl,
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = "Password Reset Request";

        message.Body = new TextPart("html")
        {
            Text = $"""
                <p>Hi {toName},</p>
                <p>We received a request to reset your password. Click the link below to set a new password:</p>
                <p><a href="{resetUrl}">Reset Password</a></p>
                <p>This link will expire in {_settings.TokenExpirationHours} hour(s).</p>
                <p>If you did not request a password reset, please ignore this email.</p>
                """
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort,
            _settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable,
            cancellationToken);

        if (!string.IsNullOrEmpty(_settings.Username))
            await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
