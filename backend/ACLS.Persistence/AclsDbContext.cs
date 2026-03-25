using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ACLS.Application.Common.Interfaces;
using ACLS.Domain.AuditLog;
using ACLS.Domain.Complaints;
using ACLS.Domain.Identity;
using ACLS.Domain.Outages;
using ACLS.Domain.Properties;
using ACLS.Domain.Residents;
using ACLS.Domain.Staff;

namespace ACLS.Persistence;

/// <summary>
/// The single EF Core DbContext for the ACLS system.
/// All entity configurations are applied via IEntityTypeConfiguration{T} classes in Configurations/.
/// Connection string is read from environment / appsettings — never hardcoded.
/// </summary>
public sealed class AclsDbContext : DbContext, IUnitOfWork
{
    public AclsDbContext(DbContextOptions<AclsDbContext> options) : base(options) { }

    // ── Property Hierarchy ──────────────────────────────────────────────────
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Unit> Units => Set<Unit>();

    // ── Identity ────────────────────────────────────────────────────────────
    public DbSet<User> Users => Set<User>();
    public DbSet<Resident> Residents => Set<Resident>();
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<InvitationToken> InvitationTokens => Set<InvitationToken>();

    // ── Complaints ──────────────────────────────────────────────────────────
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<Media> Media => Set<Media>();
    public DbSet<WorkNote> WorkNotes => Set<WorkNote>();

    // ── Outages ─────────────────────────────────────────────────────────────
    public DbSet<Outage> Outages => Set<Outage>();

    // ── Audit Log ───────────────────────────────────────────────────────────
    public DbSet<AuditEntry> AuditLog => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> implementations in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AclsDbContext).Assembly);
    }

    // ── IUnitOfWork ─────────────────────────────────────────────────────────

    private IDbContextTransaction? _currentTransaction;

    public async Task BeginTransactionAsync(CancellationToken ct)
    {
        _currentTransaction = await Database.BeginTransactionAsync(ct);
    }

    public async Task CommitTransactionAsync(CancellationToken ct)
    {
        if (_currentTransaction is null)
            throw new InvalidOperationException("No active transaction to commit.");

        await SaveChangesAsync(ct);
        await _currentTransaction.CommitAsync(ct);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken ct)
    {
        if (_currentTransaction is null) return;

        await _currentTransaction.RollbackAsync(ct);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }
}
