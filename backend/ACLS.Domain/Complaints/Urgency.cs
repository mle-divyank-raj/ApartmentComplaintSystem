namespace ACLS.Domain.Complaints;

/// <summary>
/// The severity level of a Complaint as assessed by the Resident at submission time.
/// Stored as nvarchar(50) string via UrgencyConverter in EF Core.
/// Column name: urgency
///
/// SOS_EMERGENCY bypasses Smart Dispatch ranking — all on-call Staff are notified simultaneously
/// and the Complaint is immediately set to ASSIGNED.
/// </summary>
public enum Urgency
{
    /// <summary>Non-urgent. No immediate impact on habitability.</summary>
    LOW,

    /// <summary>Noticeable issue. Should be resolved within normal SLA.</summary>
    MEDIUM,

    /// <summary>Significant issue affecting comfort or safety. Prioritise.</summary>
    HIGH,

    /// <summary>
    /// Immediate threat to life or property (fire, flood, gas leak).
    /// Bypasses standard queue. Notifies all on-call Staff simultaneously.
    /// </summary>
    SOS_EMERGENCY
}
