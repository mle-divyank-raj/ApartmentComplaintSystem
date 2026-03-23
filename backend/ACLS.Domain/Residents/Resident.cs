using ACLS.SharedKernel;

namespace ACLS.Domain.Residents;

/// <summary>
/// A person who occupies a Unit within a Building within a Property.
/// Extends Users via a one-to-one FK (UserId). DomainPropertyId is obtained via Users.PropertyId.
/// Table: Residents
/// </summary>
public sealed class Resident : EntityBase
{
    public int ResidentId { get; private set; }
    public int UserId { get; private set; }
    public int UnitId { get; private set; }
    public DateOnly? LeaseStart { get; private set; }
    public DateOnly? LeaseEnd { get; private set; }

    /// <summary>Private parameterless constructor for EF Core.</summary>
    private Resident() { }

    /// <summary>
    /// Creates a new Resident record linked to an existing User and a Unit.
    /// LeaseStart and LeaseEnd are optional at creation time.
    /// </summary>
    public static Resident Create(
        int userId,
        int unitId,
        DateOnly? leaseStart = null,
        DateOnly? leaseEnd = null)
    {
        Guard.Against.NegativeOrZero(userId, nameof(userId));
        Guard.Against.NegativeOrZero(unitId, nameof(unitId));

        return new Resident
        {
            UserId = userId,
            UnitId = unitId,
            LeaseStart = leaseStart,
            LeaseEnd = leaseEnd
        };
    }

    /// <summary>Updates the lease dates for this Resident.</summary>
    public void UpdateLeaseDates(DateOnly? leaseStart, DateOnly? leaseEnd)
    {
        LeaseStart = leaseStart;
        LeaseEnd = leaseEnd;
    }
}
