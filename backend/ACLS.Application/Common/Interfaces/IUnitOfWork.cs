namespace ACLS.Application.Common.Interfaces;

/// <summary>
/// Abstraction over database transaction control.
/// Implemented by AclsDbContext in ACLS.Persistence.
/// Used exclusively by TransactionBehaviour — never called directly in handlers.
/// </summary>
public interface IUnitOfWork
{
    Task BeginTransactionAsync(CancellationToken ct);
    Task CommitTransactionAsync(CancellationToken ct);
    Task RollbackTransactionAsync(CancellationToken ct);
}
