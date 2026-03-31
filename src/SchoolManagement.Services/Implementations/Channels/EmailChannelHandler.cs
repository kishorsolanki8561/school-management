using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Entities;
using SchoolManagement.Models.Enums;
using SchoolManagement.Services.Helpers;
using SchoolManagement.Services.Interfaces;

namespace SchoolManagement.Services.Implementations.Channels;

public sealed class EmailChannelHandler : INotificationChannelHandler
{
    public NotificationChannel Channel => NotificationChannel.Email;

    private readonly SchoolManagementDbContext _db;
    private readonly IConfiguration            _config;

    public EmailChannelHandler(SchoolManagementDbContext db, IConfiguration config)
    {
        _db     = db;
        _config = config;
    }

    public async Task<ChannelResult> SendAsync(
        int                   orgId,
        NotificationTemplate? template,
        NotificationRequest   request,
        CancellationToken     ct = default)
    {
        if (template is null)
            return Fail("Email template not configured.");

        if (string.IsNullOrWhiteSpace(request.ToEmail))
            return Fail("No email address provided.");

        // Resolve SMTP — org config first, then appsettings
        var orgConfig = await _db.OrgNotificationConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == orgId
                                   && x.Channel == NotificationChannel.Email
                                   && x.IsActive, ct);

        string host, username, password, fromAddress;
        int    port;
        bool   ssl;
        string? fromName;

        if (orgConfig?.SmtpHost is not null)
        {
            host        = orgConfig.SmtpHost;
            port        = orgConfig.SmtpPort ?? 587;
            username    = orgConfig.SmtpUsername ?? string.Empty;
            password    = orgConfig.SmtpPassword ?? string.Empty;
            fromAddress = orgConfig.FromAddress ?? string.Empty;
            fromName    = orgConfig.FromName;
            ssl         = orgConfig.EnableSsl ?? true;
        }
        else
        {
            host = _config["Mail:SmtpHost"] ?? string.Empty;
            if (string.IsNullOrEmpty(host))
                return Fail("Email not configured for this org.");

            port        = int.Parse(_config["Mail:SmtpPort"] ?? "587");
            username    = _config["Mail:Username"] ?? string.Empty;
            password    = _config["Mail:Password"] ?? string.Empty;
            fromAddress = _config["Mail:FromAddress"] ?? string.Empty;
            fromName    = _config["Mail:FromName"];
            ssl         = bool.Parse(_config["Mail:EnableSsl"] ?? "true");
        }

        var subject = TemplatePlaceholder.Apply(template.Subject, request.Placeholders);
        var body    = TemplatePlaceholder.Apply(template.Body,    request.Placeholders);

        try
        {
            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl   = ssl,
            };

            var from = fromName is not null
                ? new MailAddress(fromAddress, fromName)
                : new MailAddress(fromAddress);

            using var message = new MailMessage
            {
                From       = from,
                Subject    = subject,
                Body       = body,
                IsBodyHtml = template.IsBodyHtml,
            };

            message.To.Add(request.ToEmail);
            AddAddresses(message.To,  template.ToAddresses);
            AddAddresses(message.CC,  template.CcAddresses);
            AddAddresses(message.Bcc, template.BccAddresses);

            await client.SendMailAsync(message, ct);
            return Ok();
        }
        catch (Exception ex)
        {
            return Fail($"Email send failed: {ex.Message}");
        }
    }

    private static void AddAddresses(MailAddressCollection col, string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return;
        foreach (var a in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            col.Add(a);
    }

    private static ChannelResult Ok()   => new(NotificationChannel.Email, true,  null);
    private static ChannelResult Fail(string msg) => new(NotificationChannel.Email, false, msg);
}
