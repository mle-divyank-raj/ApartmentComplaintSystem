namespace ACLS.Domain.Staff;

/// <summary>
/// Repository interface for StaffMember persistence operations.
/// Note: StaffMember.PropertyId resolves through the associated User.PropertyId.
/// All property-scoped queries join through User to apply the PropertyId filter.
/// Defined in Domain; implemented in ACLS.Persistence.Repositories.
/// </summary>
public interface IStaffRepository
{
    /// <summary>
    /// Retrieves a StaffMember by primary key, scoped to the specified Property.
    /// PropertyId filter is applied via the associated User.
    /// </summary>
    Task<StaffMember?> GetByIdAsync(int staffMemberId, int propertyId, CancellationToken ct);

    /// <summary>
    /// Retrieves a StaffMember by their associated UserId, scoped to the Property.
    /// </summary>
    Task<StaffMember?> GetByUserIdAsync(int userId, int propertyId, CancellationToken ct);

    /// <summary>
    /// Retrieves all StaffMembers with Availability = AVAILABLE, scoped to the Property.
    /// Used by IDispatchService.FindOptimalStaffAsync to build the candidate pool.
    /// </summary>
    Task<IReadOnlyList<StaffMember>> GetAvailableAsync(int propertyId, CancellationToken ct);

    /// <summary>Retrieves all StaffMembers scoped to the specified Property.</summary>
    Task<IReadOnlyList<StaffMember>> GetAllByPropertyAsync(int propertyId, CancellationToken ct);

    /// <summary>Persists a new StaffMember.</summary>
    Task AddAsync(StaffMember staffMember, CancellationToken ct);

    /// <summary>Persists changes to an existing StaffMember (e.g. Availability or AverageRating updates).</summary>
    Task UpdateAsync(StaffMember staffMember, CancellationToken ct);
}
