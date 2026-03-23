using Microsoft.EntityFrameworkCore;
using ACLS.Domain.Residents;

namespace ACLS.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IResidentRepository.
/// PropertyId filter is applied via join through the associated User — Resident.PropertyId
/// resolves through Users.PropertyId.
/// </summary>
public sealed class ResidentRepository : IResidentRepository
{
    private readonly AclsDbContext _db;

    public ResidentRepository(AclsDbContext db) => _db = db;

    public async Task<Resident?> GetByIdAsync(int residentId, int propertyId, CancellationToken ct)
        => await _db.Residents
            .Where(r => r.ResidentId == residentId)
            .Join(_db.Users.Where(u => u.PropertyId == propertyId),
                  r => r.UserId, u => u.UserId, (r, _) => r)
            .FirstOrDefaultAsync(ct);

    public async Task<Resident?> GetByUnitAsync(int unitId, int propertyId, CancellationToken ct)
        => await _db.Residents
            .Where(r => r.UnitId == unitId)
            .Join(_db.Users.Where(u => u.PropertyId == propertyId),
                  r => r.UserId, u => u.UserId, (r, _) => r)
            .FirstOrDefaultAsync(ct);

    public async Task<Resident?> GetByUserIdAsync(int userId, int propertyId, CancellationToken ct)
        => await _db.Residents
            .Where(r => r.UserId == userId)
            .Join(_db.Users.Where(u => u.PropertyId == propertyId && u.UserId == userId),
                  r => r.UserId, u => u.UserId, (r, _) => r)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Resident>> GetAllByPropertyAsync(int propertyId, CancellationToken ct)
        => await _db.Residents
            .Join(_db.Users.Where(u => u.PropertyId == propertyId),
                  r => r.UserId, u => u.UserId, (r, _) => r)
            .ToListAsync(ct);

    public async Task AddAsync(Resident resident, CancellationToken ct)
    {
        await _db.Residents.AddAsync(resident, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Resident resident, CancellationToken ct)
    {
        _db.Residents.Update(resident);
        await _db.SaveChangesAsync(ct);
    }
}
