using ACLS.Domain.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ACLS.Infrastructure.Notifications;

/// <summary>
/// SMS delivery channel. Reads the provider API key from ACLS_NOTIFICATION_KEY and
/// the sender phone number from ACLS_NOTIFICATION_SMS_FROM per environment_config.md Section 3.4.
/// Sender number must be in E.164 format (e.g. +14155552671).
/// Stub implementation: replace the SendAsync body with a Twilio or AWS SNS client call
/// in production. All secrets must be stored in Azure Key Vault — never in appsettings.json.
/// </summary>
internal sealed class SmsNotificationProvider : INotificationChannel
{
    private readonly string _apiKey;
    private readonly string _fromNumber;
    private readonly ILogger<SmsNotificationProvider> _logger;

    public NotificationChannel ChannelType => NotificationChannel.SMS;

    public SmsNotificationProvider(
        IConfiguration configuration,
        ILogger<SmsNotificationProvider> logger)
    {
        _apiKey = configuration["ACLS_NOTIFICATION_KEY"]
            ?? throw new InvalidOperationException(
                "ACLS_NOTIFICATION_KEY is not configured. Set it as an environment variable or in Azure Key Vault.");

        _fromNumber = configuration["ACLS_NOTIFICATION_SMS_FROM"]
            ?? throw new InvalidOperationException(
                "ACLS_NOTIFICATION_SMS_FROM is not configured. Set it as an environment variable or in Azure Key Vault.");

        _logger = logger;
    }

    /// <summary>
    /// Sends an SMS to the specified E.164 phone number.
    /// Stub: logs the intent. Replace with Twilio / AWS SNS client in production.
    /// The API key (_apiKey) is available for use with the chosen provider.
    /// </summary>
    public Task SendAsync(string recipient, string subject, string body, CancellationToken ct)
    {
        _logger.LogInformation(
            "SMS notification dispatched | To: {Recipient} | From: {From} | Body: {Body}",
            recipient, _fromNumber, body);

        // Production: await twilioClient.SendSmsAsync(_apiKey, _fromNumber, recipient, body, ct);
        return Task.CompletedTask;
    }
}
