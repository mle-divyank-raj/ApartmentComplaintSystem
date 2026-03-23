namespace ACLS.Contracts.Outages;

public sealed class DeclareOutageRequest
{
    public string Title { get; init; } = string.Empty;
    public string OutageType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
}
