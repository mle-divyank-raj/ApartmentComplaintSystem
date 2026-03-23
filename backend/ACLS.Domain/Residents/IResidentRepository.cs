namespace ACLS.Domain.Residents;

/// <summary>
/// Repository interface for Resident persistence operations.
/// All query methods filter by propertyId — Resident.PropertyId resolves via User.PropertyId.
/// Defined in Domain; implemented in ACLS.Persistence.Repositories.
/// </summary>
public interface IResidentRepository
{
    /// <summary>Retrieves a Resident by primary key, scoped to the specified Property.</summary>
    Task<Resident?> GetByIdAsync(int residentId, int propertyId, CancellationToken ct);

    /// <summary>Retrieves the Resident occupying the specified Unit, scoped to the Property.</summary>
    Task<Resident?> GetByUnitAsync(int unitId, int propertyId, CancellationToken ct);

    /// <summary>Retrieves a Resident by their associated UserId, scoped to the Property.</summary>
    Task<Resident?> GetByUserIdAsync(int userId, int propertyId, CancellationToken ct);

    /// <summary>Retrieves all Residents scoped to the specified Property.</summary>
    Task<IReadOnlyList<Resident>> GetAllByPropertyAsync(int propertyId, CancellationToken ct);

    /// <summary>Persists a new Resident.</summary>
    Task AddAsync(Resident resident, CancellationToken ct);

    /// <summary>Persists changes to an existing Resident.</summary>
    Task UpdateAsync(Resident resident, CancellationToken ct);
}
