namespace ACLS.Contracts.Staff;

/// <summary>Valid values: AVAILABLE, ON_BREAK, OFF_DUTY. BUSY cannot be set manually.</summary>
public sealed class UpdateAvailabilityRequest
{
    public string Availability { get; init; } = string.Empty;
}
