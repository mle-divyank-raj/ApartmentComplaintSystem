namespace ACLS.Contracts.Complaints;

/// <summary>
/// Multipart/form-data request for resolving a complaint.
/// Text fields only. Completion photos are bound separately by the controller.
/// </summary>
public sealed class ResolveComplaintRequest
{
    public string ResolutionNotes { get; init; } = string.Empty;
}
