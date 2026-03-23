using Microsoft.EntityFrameworkCore;
using ACLS.Domain.Properties;

namespace ACLS.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IPropertyRepository.
/// Properties, Buildings, and Units form the tenancy root hierarchy.
/// No PropertyId filter is applied when querying the root Property table itself.
/// Child entity queries (Buildings, Units) are filtered by the parent hierarchy.
/// </summary>
public sealed class PropertyRepository : IPropertyRepository
{
    private readonly AclsDbContext _db;

    public PropertyRepository(AclsDbContext db) => _db = db;

    // ── Property ─────────────────────────────────────────────────────────────

    public async Task<Property?> GetByIdAsync(int propertyId, CancellationToken ct)
        => await _db.Properties.FindAsync([propertyId], ct);

    public async Task<IReadOnlyList<Property>> GetAllAsync(CancellationToken ct)
        => await _db.Properties
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

    public async Task AddAsync(Property property, CancellationToken ct)
    {
        await _db.Properties.AddAsync(property, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Property property, CancellationToken ct)
    {
        _db.Properties.Update(property);
        await _db.SaveChangesAsync(ct);
    }

    // ── Building ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<Building>> GetBuildingsByPropertyAsync(int propertyId, CancellationToken ct)
        => await _db.Buildings
            .Where(b => b.PropertyId == propertyId)
            .OrderBy(b => b.Name)
            .ToListAsync(ct);

    public async Task<Building?> GetBuildingByIdAsync(int buildingId, CancellationToken ct)
        => await _db.Buildings.FindAsync([buildingId], ct);

    public async Task AddBuildingAsync(Building building, CancellationToken ct)
    {
        await _db.Buildings.AddAsync(building, ct);
        await _db.SaveChangesAsync(ct);
    }

    // ── Unit ─────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<Unit>> GetUnitsByBuildingAsync(int buildingId, CancellationToken ct)
        => await _db.Units
            .Where(u => u.BuildingId == buildingId)
            .OrderBy(u => u.Floor)
            .ThenBy(u => u.UnitNumber)
            .ToListAsync(ct);

    public async Task<Unit?> GetUnitByIdAsync(int unitId, CancellationToken ct)
        => await _db.Units.FindAsync([unitId], ct);

    public async Task AddUnitAsync(Unit unit, CancellationToken ct)
    {
        await _db.Units.AddAsync(unit, ct);
        await _db.SaveChangesAsync(ct);
    }
}
