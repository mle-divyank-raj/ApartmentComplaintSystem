using ACLS.Domain.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ACLS.Infrastructure.Notifications;

/// <summary>
/// Email delivery channel. Reads the provider API key from ACLS_NOTIFICATION_KEY and
/// the sender address from ACLS_NOTIFICATION_EMAIL_FROM per environment_config.md Section 3.4.
/// Stub implementation: replace the SendAsync body with a SendGrid or SMTP client call
/// in production. All secrets must be stored in Azure Key Vault — never in appsettings.json.
/// </summary>
internal sealed class EmailNotificationProvider : INotificationChannel
{
    private readonly string _apiKey;
    private readonly string _fromAddress;
    private readonly ILogger<EmailNotificationProvider> _logger;

    public NotificationChannel ChannelType => NotificationChannel.Email;

    public EmailNotificationProvider(
        IConfiguration configuration,
        ILogger<EmailNotificationProvider> logger)
    {
        _apiKey = configuration["ACLS_NOTIFICATION_KEY"]
            ?? throw new InvalidOperationException(
                "ACLS_NOTIFICATION_KEY is not configured. Set it as an environment variable or in Azure Key Vault.");

        _fromAddress = configuration["ACLS_NOTIFICATION_EMAIL_FROM"]
            ?? throw new InvalidOperationException(
                "ACLS_NOTIFICATION_EMAIL_FROM is not configured. Set it as an environment variable or in Azure Key Vault.");

        _logger = logger;
    }

    /// <summary>
    /// Sends an email to the specified address.
    /// Stub: logs the intent. Replace with SendGrid / SMTP client in production.
    /// The API key (_apiKey) is available for use with the chosen provider.
    /// </summary>
    public Task SendAsync(string recipient, string subject, string body, CancellationToken ct)
    {
        _logger.LogInformation(
            "Email notification dispatched | To: {Recipient} | From: {From} | Subject: {Subject}",
            recipient, _fromAddress, subject);

        // Production: await emailClient.SendEmailAsync(_apiKey, _fromAddress, recipient, subject, body, ct);
        return Task.CompletedTask;
    }
}
