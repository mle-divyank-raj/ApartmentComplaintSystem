using ACLS.SharedKernel;

namespace ACLS.Domain.Properties;

/// <summary>
/// A physical structure within a Property. A Property contains one or more Buildings.
/// A Building contains one or more Units. Never the root of data ownership — Property is.
/// Table: Buildings
/// </summary>
public sealed class Building : EntityBase
{
    public int BuildingId { get; private set; }
    public int PropertyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    /// <summary>Private parameterless constructor for EF Core.</summary>
    private Building() { }

    /// <summary>Creates a new Building within the specified Property.</summary>
    public static Building Create(int propertyId, string name)
    {
        Guard.Against.NegativeOrZero(propertyId, nameof(propertyId));
        Guard.Against.NullOrWhiteSpace(name, nameof(name));

        return new Building
        {
            PropertyId = propertyId,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Updates the building's display name or identifier.</summary>
    public void UpdateName(string name)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Name = name;
    }
}
