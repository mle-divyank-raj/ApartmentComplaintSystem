using ACLS.Domain.Identity;

namespace ACLS.Application.Common.Interfaces;

/// <summary>
/// Interface for generating JWT bearer tokens for authenticated users.
/// Defined in Application; implemented in ACLS.Infrastructure.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a signed JWT bearer token for the given user.
    /// Claims included: sub (UserId), email, role, property_id.
    /// </summary>
    /// <returns>The signed token string and its UTC expiry.</returns>
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}
