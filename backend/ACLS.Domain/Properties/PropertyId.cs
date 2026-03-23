using ACLS.SharedKernel;

namespace ACLS.Domain.Properties;

/// <summary>
/// Strongly-typed identifier for a Property. The mandatory multi-tenancy discriminator
/// on every property-scoped entity and repository query.
/// Wraps a plain int for type safety at the domain service level.
/// JWT claim name: "property_id"
/// </summary>
public sealed class PropertyId : ValueObject
{
    /// <summary>The underlying integer value.</summary>
    public int Value { get; }

    private PropertyId(int value) => Value = value;

    /// <summary>
    /// Creates a PropertyId from a raw integer.
    /// Throws if the value is zero or negative — PropertyIds are always positive.
    /// </summary>
    public static PropertyId From(int value)
    {
        Guard.Against.NegativeOrZero(value, nameof(value));
        return new PropertyId(value);
    }

    /// <summary>Implicit conversion to int for use in EF Core LINQ queries.</summary>
    public static implicit operator int(PropertyId propertyId) => propertyId.Value;

    /// <summary>Explicit conversion from int for use where type safety is required.</summary>
    public static explicit operator PropertyId(int value) => From(value);

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
