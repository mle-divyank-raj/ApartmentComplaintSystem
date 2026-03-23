namespace ACLS.Domain.Complaints;

/// <summary>
/// Repository interface for Complaint persistence operations.
/// Every query method that returns property-scoped data requires a propertyId parameter.
/// propertyId filter is the FIRST Where clause applied in every implementation — failure to do so
/// constitutes a multi-tenancy data breach.
/// Defined in Domain; implemented in ACLS.Persistence.Repositories.
/// </summary>
public interface IComplaintRepository
{
    /// <summary>
    /// Retrieves a Complaint by primary key, scoped to the Property.
    /// Includes Media and WorkNotes navigation collections.
    /// Returns null if the complaint does not exist OR belongs to a different property (404 rule).
    /// </summary>
    Task<Complaint?> GetByIdAsync(int complaintId, int propertyId, CancellationToken ct);

    /// <summary>
    /// Retrieves a pageable, filterable list of all Complaints scoped to the Property.
    /// Supports filtering by Status, Urgency, Category, date range, and free-text search.
    /// </summary>
    Task<IReadOnlyList<Complaint>> GetAllAsync(
        int propertyId,
        ComplaintQueryOptions? options,
        CancellationToken ct);

    /// <summary>
    /// Retrieves all Complaints submitted by the specified Resident, scoped to the Property.
    /// </summary>
    Task<IReadOnlyList<Complaint>> GetByResidentAsync(
        int residentId,
        int propertyId,
        CancellationToken ct);

    /// <summary>
    /// Retrieves all Complaints currently or previously assigned to the specified StaffMember,
    /// scoped to the Property.
    /// </summary>
    Task<IReadOnlyList<Complaint>> GetByStaffMemberAsync(
        int staffMemberId,
        int propertyId,
        CancellationToken ct);

    /// <summary>
    /// Retrieves all Complaints for the specified Unit (for unit complaint history),
    /// scoped to the Property.
    /// </summary>
    Task<IReadOnlyList<Complaint>> GetByUnitAsync(
        int unitId,
        int propertyId,
        CancellationToken ct);

    /// <summary>Persists a new Complaint.</summary>
    Task AddAsync(Complaint complaint, CancellationToken ct);

    /// <summary>Persists changes to an existing Complaint (status changes, ETA, resolution, etc.).</summary>
    Task UpdateAsync(Complaint complaint, CancellationToken ct);

    /// <summary>Persists a new Media record linked to an existing Complaint.</summary>
    Task AddMediaAsync(Media media, CancellationToken ct);

    /// <summary>Persists a new WorkNote linked to an existing Complaint.</summary>
    Task AddWorkNoteAsync(WorkNote workNote, CancellationToken ct);
}
