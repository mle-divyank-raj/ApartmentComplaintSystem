using MediatR;
using Microsoft.Extensions.Logging;

namespace ACLS.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that logs the start and completion of every request.
/// Logs request name on entry. Logs elapsed milliseconds on exit (success or failure).
/// Does not suppress exceptions — infrastructure failures propagate normally.
/// </summary>
public sealed class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

    public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("ACLS Request: {RequestName} {@Request}", requestName, request);

        var start = System.Diagnostics.Stopwatch.GetTimestamp();

        var response = await next();

        var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(start);

        _logger.LogInformation(
            "ACLS Request completed: {RequestName} in {ElapsedMs}ms",
            requestName,
            elapsed.TotalMilliseconds);

        return response;
    }
}
