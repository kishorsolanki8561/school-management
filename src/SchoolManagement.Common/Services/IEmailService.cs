namespace SchoolManagement.Common.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetUrl,
        CancellationToken cancellationToken = default);
}
