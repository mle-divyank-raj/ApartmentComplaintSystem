namespace ACLS.Domain.Staff;

/// <summary>
/// The current work availability state of a StaffMember.
/// Stored as nvarchar(50) strings via StaffStateConverter in EF Core.
/// Only AVAILABLE staff are eligible for Smart Dispatch recommendations.
/// Column name: availability
/// </summary>
public enum StaffState
{
    /// <summary>Staff Member is on duty and can accept new assignments.</summary>
    AVAILABLE,

    /// <summary>Staff Member is currently assigned to an active Complaint. Not eligible for Dispatch.</summary>
    BUSY,

    /// <summary>Staff Member is temporarily unavailable. Not eligible for Dispatch.</summary>
    ON_BREAK,

    /// <summary>Staff Member is not on shift. Not eligible for Dispatch.</summary>
    OFF_DUTY
}
