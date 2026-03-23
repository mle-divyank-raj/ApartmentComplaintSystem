namespace ACLS.SharedKernel;

/// <summary>
/// Immutable error value. Carries a machine-readable Code and a human-readable Message.
/// Error codes follow the convention: "[BoundedContext].[ErrorName]"
/// e.g. "Complaint.NotFound", "Staff.Unavailable", "Auth.InvalidToken".
/// Used as the failure payload in Result and Result{T}.
/// </summary>
public sealed record Error(string Code, string Message)
{
    /// <summary>Represents the absence of an error. Used on Result.Success() instances.</summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>
    /// A generic unexpected failure — use sparingly. Prefer domain-specific errors.
    /// </summary>
    public static Error Unexpected(string detail) =>
        new("Unexpected", $"An unexpected error occurred: {detail}");
}
