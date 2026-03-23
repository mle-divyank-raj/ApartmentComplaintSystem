using ACLS.Application.Common.Interfaces;

namespace ACLS.Api.Services;

/// <summary>
/// Scoped request context that holds the authenticated user's PropertyId and UserId.
/// Populated by TenancyMiddleware (which reads JWT claims) after UseAuthentication().
/// 
/// Interface (ICurrentPropertyContext) is in ACLS.Application so handlers can consume it
/// without referencing ACLS.Api.
/// </summary>
public sealed class CurrentPropertyContext : ICurrentPropertyContext
{
    private int _propertyId;
    private int _userId;
    private bool _isSet;

    public int PropertyId
    {
        get
        {
            if (!_isSet)
                throw new InvalidOperationException(
                    "ICurrentPropertyContext has not been initialised. " +
                    "Ensure TenancyMiddleware runs after UseAuthentication().");
            return _propertyId;
        }
    }

    public int UserId
    {
        get
        {
            if (!_isSet)
                throw new InvalidOperationException(
                    "ICurrentPropertyContext has not been initialised. " +
                    "Ensure TenancyMiddleware runs after UseAuthentication().");
            return _userId;
        }
    }

    /// <summary>
    /// Called exclusively by TenancyMiddleware to populate context from JWT claims.
    /// Marked internal so no controller or handler can call it directly.
    /// </summary>
    internal void SetContext(int propertyId, int userId)
    {
        _propertyId = propertyId;
        _userId = userId;
        _isSet = true;
    }
}
