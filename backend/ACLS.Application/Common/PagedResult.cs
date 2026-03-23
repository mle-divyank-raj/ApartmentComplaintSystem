namespace ACLS.Application.Common;

/// <summary>
/// Generic paginated result container used by list query handlers.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
