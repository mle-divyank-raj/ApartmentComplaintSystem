using ACLS.Application.Common.Interfaces;
using MediatR;

namespace ACLS.Application.Common.Behaviours;

/// <summary>
/// Marker interface for commands that require a database transaction.
/// Apply to IRequest implementations in Commands/ that must be atomic
/// (i.e. span multiple repository writes that must commit or roll back together).
/// Example: AssignComplaintCommand, ResolveComplaintCommand.
/// </summary>
public interface ITransactionalCommand { }

/// <summary>
/// MediatR pipeline behaviour that wraps transactional commands in a database transaction.
/// Only activates for requests that implement ITransactionalCommand.
/// The transaction is committed after the handler returns successfully.
/// If the handler throws, the transaction is rolled back automatically.
///
/// Requires IUnitOfWork to be registered in DI (implemented by AclsDbContext in ACLS.Persistence).
/// </summary>
public sealed class TransactionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehaviour(IUnitOfWork unitOfWork)
        => _unitOfWork = unitOfWork;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ITransactionalCommand)
            return await next();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return response;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
