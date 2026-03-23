using FluentValidation;
using MediatR;

namespace ACLS.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that runs FluentValidation validators registered for the request type.
/// Runs BEFORE the handler. If any validator returns failures, the pipeline short-circuits and
/// returns a failure Result without invoking the handler.
/// Validators are discovered automatically via DI — each IValidator{T} for the request type
/// registered in the DI container will be invoked.
/// </summary>
public sealed class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationTasks = _validators
            .Select(v => v.ValidateAsync(context, cancellationToken));

        var results = await Task.WhenAll(validationTasks);

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}
