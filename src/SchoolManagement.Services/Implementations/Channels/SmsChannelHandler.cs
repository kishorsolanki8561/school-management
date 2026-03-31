using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Entities;
using SchoolManagement.Models.Enums;
using SchoolManagement.Services.Helpers;
using SchoolManagement.Services.Interfaces;

namespace SchoolManagement.Services.Implementations.Channels;

public sealed class SmsChannelHandler : INotificationChannelHandler
{
    public NotificationChannel Channel => NotificationChannel.SMS;

    private readonly SchoolManagementDbContext _db;
    private readonly IConfiguration            _config;
    private readonly IHttpClientFactory        _httpClientFactory;

    public SmsChannelHandler(
        SchoolManagementDbContext db,
        IConfiguration            config,
        IHttpClientFactory        httpClientFactory)
    {
        _db                = db;
        _config            = config;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ChannelResult> SendAsync(
        int                   orgId,
        NotificationTemplate? template,
        NotificationRequest   request,
        CancellationToken     ct = default)
    {
        if (template is null)
            return Fail("SMS template not configured.");

        if (string.IsNullOrWhiteSpace(request.ToPhone))
            return Fail("No phone number provided.");

        var orgConfig = await _db.OrgNotificationConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == orgId
                                   && x.Channel == NotificationChannel.SMS
                                   && x.IsActive, ct);

        // Resolve provider + credentials
        SmsProvider provider;
        string      apiKey, senderNumber, senderName;
        string?     accountSid, authToken;

        if (orgConfig?.SmsProvider is not null)
        {
            provider     = orgConfig.SmsProvider.Value;
            apiKey       = orgConfig.ApiKey       ?? string.Empty;
            accountSid   = orgConfig.AccountSid;
            authToken    = orgConfig.AuthToken;
            senderNumber = orgConfig.SenderNumber ?? string.Empty;
            senderName   = orgConfig.SenderName   ?? string.Empty;
        }
        else
        {
            // OwnerAdmin appsettings fallback (Infobip)
            apiKey = _config["Sms:Infobip:ApiKey"] ?? string.Empty;
            if (string.IsNullOrEmpty(apiKey))
                return Fail("SMS not configured for this org.");

            provider     = SmsProvider.Infobip;
            accountSid   = null;
            authToken    = null;
            senderNumber = _config["Sms:Infobip:SenderNumber"] ?? string.Empty;
            senderName   = _config["Sms:Infobip:SenderName"]   ?? string.Empty;
        }

        var body = TemplatePlaceholder.Apply(template.Body, request.Placeholders);

        try
        {
            return provider switch
            {
                SmsProvider.Twilio      => await SendTwilioAsync(accountSid!, authToken!, senderNumber, request.ToPhone, body, ct),
                SmsProvider.Infobip     => await SendInfobipAsync(apiKey, senderName, request.ToPhone, body, ct),
                SmsProvider.SslWireless => await SendSslWirelessAsync(apiKey, senderNumber, request.ToPhone, body, ct),
                _                       => Fail($"Unknown SMS provider: {provider}"),
            };
        }
        catch (Exception ex)
        {
            return Fail($"SMS send failed: {ex.Message}");
        }
    }

    // ── Twilio ────────────────────────────────────────────────────────────────
    private async Task<ChannelResult> SendTwilioAsync(
        string accountSid, string authToken,
        string from, string to, string body, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"]   = to,
            ["From"] = from,
            ["Body"] = body,
        });

        var response = await client.PostAsync(
            $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json",
            content, ct);

        return response.IsSuccessStatusCode
            ? Ok()
            : Fail($"Twilio error: {response.StatusCode}");
    }

    // ── Infobip ───────────────────────────────────────────────────────────────
    private async Task<ChannelResult> SendInfobipAsync(
        string apiKey, string senderName, string to, string body, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"App {apiKey}");
        client.DefaultRequestHeaders.Add("Accept", "application/json");

        var payload = new
        {
            messages = new[]
            {
                new
                {
                    destinations = new[] { new { to } },
                    @from        = senderName,
                    text         = body,
                }
            }
        };

        var json    = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("https://api.infobip.com/sms/2/text/advanced", content, ct);

        return response.IsSuccessStatusCode
            ? Ok()
            : Fail($"Infobip error: {response.StatusCode}");
    }

    // ── SSL Wireless (BD) ─────────────────────────────────────────────────────
    private async Task<ChannelResult> SendSslWirelessAsync(
        string apiKey, string senderNumber, string to, string body, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();

        var payload = new
        {
            api_key = apiKey,
            senderid = senderNumber,
            number   = to,
            message  = body,
        };

        var json    = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("https://smsplus.sslwireless.com/api/v3/send-sms", content, ct);

        return response.IsSuccessStatusCode
            ? Ok()
            : Fail($"SSL Wireless error: {response.StatusCode}");
    }

    private static ChannelResult Ok()              => new(NotificationChannel.SMS, true,  null);
    private static ChannelResult Fail(string msg)  => new(NotificationChannel.SMS, false, msg);
}
