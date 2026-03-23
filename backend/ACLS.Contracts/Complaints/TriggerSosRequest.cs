namespace ACLS.Contracts.Complaints;

public sealed class TriggerSosRequest
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool PermissionToEnter { get; init; }
}
