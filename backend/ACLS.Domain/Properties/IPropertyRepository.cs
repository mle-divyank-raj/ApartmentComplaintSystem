namespace ACLS.Domain.Properties;

/// <summary>
/// Repository interface for Property hierarchy entities (Property, Building, Unit).
/// Properties and their children (Buildings, Units) are the tenancy root — no PropertyId filter
/// is applied when reading the root entities themselves.
/// Defined in Domain; implemented in ACLS.Persistence.Repositories.
/// </summary>
public interface IPropertyRepository
{
    // ── Property ─────────────────────────────────────────────────────────────

    /// <summary>Retrieves a Property by its primary key.</summary>
    Task<Property?> GetByIdAsync(int propertyId, CancellationToken ct);

    /// <summary>Retrieves all active Properties on the platform.</summary>
    Task<IReadOnlyList<Property>> GetAllAsync(CancellationToken ct);

    /// <summary>Persists a new Property.</summary>
    Task AddAsync(Property property, CancellationToken ct);

    /// <summary>Persists changes to an existing Property.</summary>
    Task UpdateAsync(Property property, CancellationToken ct);

    // ── Building ─────────────────────────────────────────────────────────────

    /// <summary>Retrieves all Buildings within a Property.</summary>
    Task<IReadOnlyList<Building>> GetBuildingsByPropertyAsync(int propertyId, CancellationToken ct);

    /// <summary>Retrieves a Building by its primary key.</summary>
    Task<Building?> GetBuildingByIdAsync(int buildingId, CancellationToken ct);

    /// <summary>Persists a new Building.</summary>
    Task AddBuildingAsync(Building building, CancellationToken ct);

    // ── Unit ─────────────────────────────────────────────────────────────────

    /// <summary>Retrieves all Units within a Building.</summary>
    Task<IReadOnlyList<Unit>> GetUnitsByBuildingAsync(int buildingId, CancellationToken ct);

    /// <summary>Retrieves a Unit by its primary key.</summary>
    Task<Unit?> GetUnitByIdAsync(int unitId, CancellationToken ct);

    /// <summary>Persists a new Unit.</summary>
    Task AddUnitAsync(Unit unit, CancellationToken ct);
}
