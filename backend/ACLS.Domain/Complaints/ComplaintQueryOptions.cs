namespace ACLS.Domain.Complaints;

/// <summary>
/// Options for filtering, sorting, and paginating Complaint list queries.
/// Passed to IComplaintRepository.GetAllAsync by the application query handler.
/// All filter fields are optional — null means "no filter on this field".
/// </summary>
public sealed record ComplaintQueryOptions(
    TicketStatus? Status = null,
    Urgency? Urgency = null,
    string? Category = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    string? Search = null,
    string? SortBy = null,
    string? SortDirection = null,
    int Page = 1,
    int PageSize = 20);
