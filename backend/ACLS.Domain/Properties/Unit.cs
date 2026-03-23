using ACLS.SharedKernel;

namespace ACLS.Domain.Properties;

/// <summary>
/// An individual apartment within a Building. A Unit belongs to exactly one Building.
/// A Resident is linked to exactly one Unit.
/// Table: Units
/// </summary>
public sealed class Unit : EntityBase
{
    public int UnitId { get; private set; }
    public int BuildingId { get; private set; }
    public string UnitNumber { get; private set; } = string.Empty;
    public int Floor { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>Private parameterless constructor for EF Core.</summary>
    private Unit() { }

    /// <summary>
    /// Creates a new Unit within a Building.
    /// UnitNumber examples: "4B", "101", "PH2". Floor 0 = ground floor.
    /// </summary>
    public static Unit Create(int buildingId, string unitNumber, int floor)
    {
        Guard.Against.NegativeOrZero(buildingId, nameof(buildingId));
        Guard.Against.NullOrWhiteSpace(unitNumber, nameof(unitNumber));
        Guard.Against.Negative(floor, nameof(floor));

        return new Unit
        {
            BuildingId = buildingId,
            UnitNumber = unitNumber,
            Floor = floor,
            CreatedAt = DateTime.UtcNow
        };
    }
}
