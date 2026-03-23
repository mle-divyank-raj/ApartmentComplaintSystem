namespace ACLS.Domain.Identity;

/// <summary>
/// Repository interface for User persistence operations.
/// Defined in Domain; implemented in ACLS.Persistence.Repositories.
/// Cross-property queries (e.g. login by email) are permitted since
/// email uniqueness is global and authentication must work before PropertyId is known.
/// </summary>
public interface IUserRepository
{
    /// <summary>Retrieves a User by their primary key. No PropertyId filter — used pre-auth.</summary>
    Task<User?> GetByIdAsync(int userId, CancellationToken ct);

    /// <summary>
    /// Retrieves a User by email address for authentication.
    /// Email is globally unique — no PropertyId filter required here.
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);

    /// <summary>Retrieves all users scoped to a specific Property.</summary>
    Task<IReadOnlyList<User>> GetAllByPropertyAsync(int propertyId, CancellationToken ct);

    /// <summary>Persists a new User.</summary>
    Task AddAsync(User user, CancellationToken ct);

    /// <summary>Persists changes to an existing User.</summary>
    Task UpdateAsync(User user, CancellationToken ct);

    /// <summary>Retrieves an InvitationToken by its token string for redemption validation.</summary>
    Task<InvitationToken?> GetInvitationTokenAsync(string token, CancellationToken ct);

    /// <summary>Persists a new InvitationToken.</summary>
    Task AddInvitationTokenAsync(InvitationToken invitationToken, CancellationToken ct);

    /// <summary>Persists changes to an existing InvitationToken (e.g. after redemption or revocation).</summary>
    Task UpdateInvitationTokenAsync(InvitationToken invitationToken, CancellationToken ct);
}
