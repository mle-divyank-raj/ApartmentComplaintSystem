namespace ACLS.Contracts.Auth;

public sealed class RegisterResidentRequest
{
    public string InvitationToken { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Phone { get; init; }
}
