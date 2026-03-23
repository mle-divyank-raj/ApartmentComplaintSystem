namespace ACLS.Domain.Outages;

/// <summary>
/// The type of service disrupted by an Outage.
/// Stored as nvarchar(50) string via OutageTypeConverter in EF Core.
/// Column name: outage_type
/// </summary>
public enum OutageType
{
    Electricity,
    Water,
    Gas,
    Internet,
    Elevator,
    Other
}
