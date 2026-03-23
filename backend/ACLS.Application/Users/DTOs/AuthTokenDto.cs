namespace ACLS.Application.Users.DTOs;

/// <summary>
/// JWT bearer token response returned after successful login or registration.
/// </summary>
public sealed record AuthTokenDto(
    string AccessToken,
    DateTime ExpiresAt,
    int UserId,
    string Role,
    int PropertyId);
