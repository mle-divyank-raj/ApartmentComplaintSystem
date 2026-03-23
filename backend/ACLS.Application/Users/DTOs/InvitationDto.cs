namespace ACLS.Application.Users.DTOs;

/// <summary>
/// DTO returned when a Manager creates a resident invitation token.
/// </summary>
public sealed record InvitationDto(
    int InvitationTokenId,
    string Email,
    int UnitId,
    string? UnitNumber,
    DateTime ExpiresAt,
    DateTime IssuedAt);
