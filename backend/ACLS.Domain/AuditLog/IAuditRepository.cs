namespace ACLS.Domain.AuditLog;

/// <summary>
/// Repository interface for AuditEntry persistence.
/// IMPORTANT: Only AddAsync is exposed. Audit log entries are immutable once written.
/// No update or delete operations exist. This is enforced by interface design, not EF configuration.
/// Defined in Domain; implemented in ACLS.Persistence.Repositories.
/// </summary>
public interface IAuditRepository
{
    /// <summary>
    /// Persists a new AuditEntry. This is the only operation available on the audit log.
    /// </summary>
    Task AddAsync(AuditEntry entry, CancellationToken ct);
}
