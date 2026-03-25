using ACLS.Domain.Complaints;
using ACLS.Domain.Identity;
using ACLS.Domain.Notifications;
using ACLS.Domain.Outages;
using ACLS.Domain.Residents;
using ACLS.Domain.Staff;
using Microsoft.Extensions.Logging;

namespace ACLS.Infrastructure.Notifications;

/// <summary>
/// Implements INotificationService using the registered INotificationChannel providers.
/// BroadcastOutageAsync and NotifyAllOnCallStaffAsync fan out concurrently via Task.WhenAll()
/// to meet NFR-12: 500 messages dispatched within 60 seconds.
/// Defined in Domain; implemented here in ACLS.Infrastructure.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly IResidentRepository _residentRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEnumerable<INotificationChannel> channels,
        IResidentRepository residentRepository,
        IUserRepository userRepository,
        ILogger<NotificationService> logger)
    {
        _channels = channels;
        _residentRepository = residentRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task NotifyResidentAsync(
        int residentId,
        string subject,
        string message,
        IEnumerable<NotificationChannel> channels,
        CancellationToken ct)
    {
        var channelSet = channels.ToHashSet();

        _logger.LogInformation(
            "Sending notification to ResidentId {ResidentId} | Subject: {Subject} | Channels: {Channels}",
            residentId, subject, string.Join(", ", channelSet));

        var tasks = _channels
            .Where(c => channelSet.Contains(c.ChannelType))
            .Select(c => c.SendAsync($"resident:{residentId}", subject, message, ct));

        await Task.WhenAll(tasks);
    }

    /// <inheritdoc />
    public async Task NotifyStaffAsync(
        int staffMemberId,
        string subject,
        string message,
        IEnumerable<NotificationChannel> channels,
        CancellationToken ct)
    {
        var channelSet = channels.ToHashSet();

        _logger.LogInformation(
            "Sending notification to StaffMemberId {StaffMemberId} | Subject: {Subject} | Channels: {Channels}",
            staffMemberId, subject, string.Join(", ", channelSet));

        var tasks = _channels
            .Where(c => channelSet.Contains(c.ChannelType))
            .Select(c => c.SendAsync($"staff:{staffMemberId}", subject, message, ct));

        await Task.WhenAll(tasks);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Fan-out is concurrent via Task.WhenAll() to meet NFR-12 (500 messages / 60 seconds).
    /// Fetches all residents from the property, then dispatches Email + SMS in parallel.
    /// </remarks>
    public async Task BroadcastOutageAsync(Outage outage, CancellationToken ct)
    {
        _logger.LogInformation(
            "BroadcastOutageAsync: Starting for OutageId {OutageId} | PropertyId {PropertyId} | Type: {OutageType}",
            outage.OutageId, outage.PropertyId, outage.OutageType);

        var residents = await _residentRepository.GetAllByPropertyAsync(outage.PropertyId, ct);

        if (residents.Count == 0)
        {
            _logger.LogWarning(
                "BroadcastOutageAsync: No residents found for PropertyId {PropertyId}; no notifications sent",
                outage.PropertyId);
            return;
        }

        var subject = $"[ACLS] Outage Alert: {outage.OutageType} — {outage.Title}";
        var body = $"A {outage.OutageType} outage has been declared at your property.\n\n" +
                   $"Details: {outage.Description}\n" +
                   $"Start: {outage.StartTime:u}" +
                   (outage.EndTime.HasValue
                       ? $"\nExpected end: {outage.EndTime.Value:u}"
                       : "\nEstimated end time: TBD");

        // Build all send tasks up-front before awaiting, then fan out concurrently (NFR-12).
        var sendTasks = residents.SelectMany(resident =>
            _channels.Select(channel =>
                SendToUserAsync(resident.UserId, channel, subject, body, ct)));

        await Task.WhenAll(sendTasks);

        _logger.LogInformation(
            "BroadcastOutageAsync: Complete | OutageId {OutageId} | Recipients: {Count}",
            outage.OutageId, residents.Count);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Concurrent fan-out via Task.WhenAll() to meet NFR-12 SOS blast requirement.
    /// All provided StaffMembers are notified simultaneously across all registered channels.
    /// </remarks>
    public async Task NotifyAllOnCallStaffAsync(
        IEnumerable<StaffMember> onCallStaff,
        Complaint complaint,
        CancellationToken ct)
    {
        var staffList = onCallStaff.ToList();

        _logger.LogInformation(
            "NotifyAllOnCallStaffAsync: SOS blast for ComplaintId {ComplaintId} | PropertyId {PropertyId} | StaffCount: {Count}",
            complaint.ComplaintId, complaint.PropertyId, staffList.Count);

        if (staffList.Count == 0)
        {
            _logger.LogWarning(
                "NotifyAllOnCallStaffAsync: No on-call staff for ComplaintId {ComplaintId}; no SOS notifications sent",
                complaint.ComplaintId);
            return;
        }

        var subject = $"[ACLS] 🚨 SOS EMERGENCY — Complaint #{complaint.ComplaintId}";
        var body = $"SOS EMERGENCY raised.\n\n" +
                   $"Complaint #{complaint.ComplaintId}: {complaint.Title}\n" +
                   $"Unit: {complaint.UnitId} | Property: {complaint.PropertyId}\n" +
                   $"Immediate response required.";

        // Build all send tasks up-front before awaiting, then fan out concurrently (NFR-12).
        var sendTasks = staffList.SelectMany(staff =>
            _channels.Select(channel =>
                SendToUserAsync(staff.UserId, channel, subject, body, ct)));

        await Task.WhenAll(sendTasks);
    }

    /// <summary>
    /// Resolves the User's contact address for the given channel type and dispatches the message.
    /// Skips silently if the User is not found or lacks a contact address for the requested channel.
    /// </summary>
    private async Task SendToUserAsync(
        int userId,
        INotificationChannel channel,
        string subject,
        string body,
        CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);

        if (user is null)
        {
            _logger.LogWarning(
                "SendToUserAsync: UserId {UserId} not found; skipping {Channel} notification",
                userId, channel.ChannelType);
            return;
        }

        string recipient = channel.ChannelType switch
        {
            NotificationChannel.Email => user.Email,
            NotificationChannel.SMS   => user.Phone ?? string.Empty,
            _                         => string.Empty
        };

        if (string.IsNullOrEmpty(recipient))
        {
            _logger.LogWarning(
                "SendToUserAsync: UserId {UserId} has no {Channel} contact; skipping",
                userId, channel.ChannelType);
            return;
        }

        await channel.SendAsync(recipient, subject, body, ct);
    }
}
