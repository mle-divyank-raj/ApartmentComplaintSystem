namespace ACLS.Domain.AuditLog;

/// <summary>
/// The action type recorded in an AuditEntry.
/// Stored as nvarchar(100) string via AuditActionConverter in EF Core.
/// Column name: action
/// </summary>
public enum AuditAction
{
    ComplaintCreated,
    ComplaintAssigned,
    ComplaintReassigned,
    ComplaintStatusChanged,
    ComplaintResolved,
    ComplaintClosed,
    SosTriggered,
    OutageDeclared,
    StaffAvailabilityChanged,
    UserInvited,
    UserDeactivated,
    UserReactivated,
    MediaUploaded,
    FeedbackSubmitted
}
