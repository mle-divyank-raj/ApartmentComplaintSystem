namespace ACLS.Application.Common.Interfaces;

/// <summary>
/// Provides the current request's property context derived from the authenticated user's JWT.
/// Populated by TenancyMiddleware (ACLS.Api) using the property_id claim.
/// Injected into command/query handlers that require multi-tenancy isolation.
/// </summary>
public interface ICurrentPropertyContext
{
    /// <summary>The property ID extracted from the authenticated user's JWT claim.</summary>
    int PropertyId { get; }

    /// <summary>The user ID extracted from the authenticated user's JWT claim.</summary>
    int UserId { get; }
}
