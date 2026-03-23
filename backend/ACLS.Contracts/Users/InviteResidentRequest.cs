namespace ACLS.Contracts.Users;

public sealed class InviteResidentRequest
{
    public string Email { get; init; } = string.Empty;
    public int UnitId { get; init; }
}
