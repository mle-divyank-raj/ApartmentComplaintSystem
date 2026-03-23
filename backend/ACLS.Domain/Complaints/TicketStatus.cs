namespace ACLS.Domain.Complaints;

/// <summary>
/// The lifecycle state of a Complaint.
/// Stored as nvarchar(50) string via TicketStatusConverter in EF Core.
/// Column name: status
///
/// Valid transitions (state machine — enforced in Complaint domain methods):
///   OPEN → ASSIGNED (Manager assigns, or SOS auto-assign)
///   ASSIGNED → EN_ROUTE (Staff accepts — AcceptAssignment)
///   EN_ROUTE → IN_PROGRESS (Staff starts work — StartWork)
///   IN_PROGRESS → RESOLVED (Staff resolves — Resolve)
///   RESOLVED → CLOSED (Resident submits feedback — Close)
///   ASSIGNED → ASSIGNED (Reassignment — Manager reassigns to different Staff)
/// </summary>
public enum TicketStatus
{
    OPEN,
    ASSIGNED,
    EN_ROUTE,
    IN_PROGRESS,
    RESOLVED,
    CLOSED
}
