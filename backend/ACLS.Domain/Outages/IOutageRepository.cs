namespace ACLS.Domain.Outages;

/// <summary>
/// Repository interface for Outage persistence operations.
/// All query methods are scoped to the specified propertyId.
/// Defined in Domain; implemented in ACLS.Persistence.Repositories.
/// </summary>
public interface IOutageRepository
{
    /// <summary>Retrieves an Outage by primary key, scoped to the Property.</summary>
    Task<Outage?> GetByIdAsync(int outageId, int propertyId, CancellationToken ct);

    /// <summary>Retrieves all Outages for a Property, ordered by DeclaredAt descending.</summary>
    Task<IReadOnlyList<Outage>> GetAllByPropertyAsync(int propertyId, CancellationToken ct);

    /// <summary>Retrieves active Outages (StartTime ≤ now and EndTime is null or > now).</summary>
    Task<IReadOnlyList<Outage>> GetActiveByPropertyAsync(int propertyId, CancellationToken ct);

    /// <summary>Persists a new Outage.</summary>
    Task AddAsync(Outage outage, CancellationToken ct);

    /// <summary>Persists changes to an existing Outage (e.g. after MarkNotificationSent or UpdateEndTime).</summary>
    Task UpdateAsync(Outage outage, CancellationToken ct);
}
