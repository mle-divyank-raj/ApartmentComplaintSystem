using Microsoft.EntityFrameworkCore;
using ACLS.Domain.Identity;

namespace ACLS.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IUserRepository.
/// GetByEmailAsync is a global search (no PropertyId filter) — required for pre-auth login.
/// All property-scoped queries filter by User.PropertyId directly.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly AclsDbContext _db;

    public UserRepository(AclsDbContext db) => _db = db;

    public async Task<User?> GetByIdAsync(int userId, CancellationToken ct)
        => await _db.Users.FindAsync([userId], ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
        => await _db.Users
            .Where(u => u.Email == email.ToLowerInvariant())
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<User>> GetAllByPropertyAsync(int propertyId, CancellationToken ct)
        => await _db.Users
            .Where(u => u.PropertyId == propertyId)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(ct);

    public async Task AddAsync(User user, CancellationToken ct)
    {
        await _db.Users.AddAsync(user, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<InvitationToken?> GetInvitationTokenAsync(string token, CancellationToken ct)
        => await _db.InvitationTokens
            .Where(t => t.Token == token)
            .FirstOrDefaultAsync(ct);

    public async Task AddInvitationTokenAsync(InvitationToken invitationToken, CancellationToken ct)
    {
        await _db.InvitationTokens.AddAsync(invitationToken, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateInvitationTokenAsync(InvitationToken invitationToken, CancellationToken ct)
    {
        _db.InvitationTokens.Update(invitationToken);
        await _db.SaveChangesAsync(ct);
    }
}
