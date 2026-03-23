using Microsoft.EntityFrameworkCore;
using ACLS.Domain.Outages;

namespace ACLS.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IOutageRepository.
/// All queries filter by PropertyId — Outages are property-scoped.
/// </summary>
public sealed class OutageRepository : IOutageRepository
{
    private readonly AclsDbContext _db;

    public OutageRepository(AclsDbContext db) => _db = db;

    public async Task<Outage?> GetByIdAsync(int outageId, int propertyId, CancellationToken ct)
        => await _db.Outages
            .Where(o => o.OutageId == outageId && o.PropertyId == propertyId)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Outage>> GetAllByPropertyAsync(int propertyId, CancellationToken ct)
        => await _db.Outages
            .Where(o => o.PropertyId == propertyId)
            .OrderByDescending(o => o.DeclaredAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Outage>> GetActiveByPropertyAsync(int propertyId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return await _db.Outages
            .Where(o => o.PropertyId == propertyId
                        && o.StartTime <= now
                        && o.EndTime >= now)
            .OrderByDescending(o => o.DeclaredAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Outage outage, CancellationToken ct)
    {
        await _db.Outages.AddAsync(outage, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Outage outage, CancellationToken ct)
    {
        _db.Outages.Update(outage);
        await _db.SaveChangesAsync(ct);
    }
}
