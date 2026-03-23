using ACLS.Domain.Identity;

namespace ACLS.Application.Users.DTOs;

/// <summary>
/// DTO for a User returned by user management queries.
/// </summary>
public sealed record UserDto(
    int UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt)
{
    public static UserDto FromDomain(User user) => new(
        user.UserId,
        user.Email,
        user.FirstName,
        user.LastName,
        user.Role.ToString(),
        user.IsActive,
        user.CreatedAt,
        user.LastLoginAt);
}
