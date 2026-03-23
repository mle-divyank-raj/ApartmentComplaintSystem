namespace ACLS.Contracts.Complaints;

/// <summary>Valid values: EN_ROUTE, IN_PROGRESS (per v1.yaml).</summary>
public sealed class UpdateComplaintStatusRequest
{
    public string Status { get; init; } = string.Empty;
}
