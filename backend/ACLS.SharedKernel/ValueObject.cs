namespace ACLS.SharedKernel;

/// <summary>
/// Abstract base class for value objects. Value objects have no identity —
/// two instances with the same atomic values are equal.
/// Subclasses override GetAtomicValues() to declare which properties participate in equality.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Returns the ordered sequence of values that define equality for this value object.
    /// Every property that participates in equality must be yielded here.
    /// </summary>
    protected abstract IEnumerable<object> GetAtomicValues();

    public bool Equals(ValueObject? other)
    {
        if (other is null || other.GetType() != GetType())
            return false;

        return GetAtomicValues().SequenceEqual(other.GetAtomicValues());
    }

    public override bool Equals(object? obj)
        => obj is ValueObject other && Equals(other);

    public override int GetHashCode()
        => GetAtomicValues()
            .Aggregate(default(int), HashCode.Combine);

    public static bool operator ==(ValueObject? left, ValueObject? right)
        => left?.Equals(right) ?? right is null;

    public static bool operator !=(ValueObject? left, ValueObject? right)
        => !(left == right);
}
