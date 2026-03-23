using ACLS.SharedKernel;

namespace ACLS.Domain.Properties;

/// <summary>
/// A physical apartment complex managed on the platform. The root of the data hierarchy
/// and the unit of multi-tenancy. All data (Residents, Staff, Complaints, Outages) belongs
/// to exactly one Property.
/// Table: Properties
/// </summary>
public sealed class Property : EntityBase
{
    public int PropertyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>Private parameterless constructor for EF Core.</summary>
    private Property() { }

    /// <summary>Creates a new Property (apartment complex) on the platform.</summary>
    public static Property Create(string name, string address)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.NullOrWhiteSpace(address, nameof(address));

        return new Property
        {
            Name = name,
            Address = address,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Deactivates the property. Users scoped to this property cannot operate.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Reactivates a previously deactivated property.</summary>
    public void Activate() => IsActive = true;

    /// <summary>Updates the display name of the property.</summary>
    public void UpdateName(string name)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Name = name;
    }

    /// <summary>Updates the street address of the property.</summary>
    public void UpdateAddress(string address)
    {
        Guard.Against.NullOrWhiteSpace(address, nameof(address));
        Address = address;
    }
}
