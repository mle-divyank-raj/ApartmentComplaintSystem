namespace ACLS.Contracts.Complaints;

/// <summary>
/// Multipart/form-data request for submitting a new maintenance complaint.
/// Text fields only. Media files are bound separately by the controller via IFormFileCollection
/// and uploaded to blob storage before the SubmitComplaintCommand is dispatched.
/// </summary>
public sealed class SubmitComplaintRequest
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Urgency { get; init; } = string.Empty;
    public bool PermissionToEnter { get; init; }
}
