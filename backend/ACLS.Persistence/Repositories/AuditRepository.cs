using ACLS.Domain.AuditLog;

namespace ACLS.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IAuditRepository.
/// Exposes only AddAsync — audit log entries are immutable once written.
/// No Update or Delete operations are permitted by interface design or by this implementation.
/// </summary>
public sealed class AuditRepository : IAuditRepository
{
    private readonly AclsDbContext _db;

    public AuditRepository(AclsDbContext db) => _db = db;

    public async Task AddAsync(AuditEntry entry, CancellationToken ct)
    {
        await _db.AuditLog.AddAsync(entry, ct);
        await _db.SaveChangesAsync(ct);
    }
}
