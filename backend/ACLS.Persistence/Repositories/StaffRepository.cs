using Microsoft.EntityFrameworkCore;
using ACLS.Domain.Staff;

namespace ACLS.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IStaffRepository.
/// PropertyId filter applied via join through Users — StaffMember.PropertyId resolves via User.PropertyId.
/// </summary>
public sealed class StaffRepository : IStaffRepository
{
    private readonly AclsDbContext _db;

    public StaffRepository(AclsDbContext db) => _db = db;

    public async Task<StaffMember?> GetByIdAsync(int staffMemberId, int propertyId, CancellationToken ct)
        => await _db.StaffMembers
            .Where(s => s.StaffMemberId == staffMemberId)
            .Join(_db.Users.Where(u => u.PropertyId == propertyId),
                  s => s.UserId, u => u.UserId, (s, _) => s)
            .FirstOrDefaultAsync(ct);

    public async Task<StaffMember?> GetByUserIdAsync(int userId, int propertyId, CancellationToken ct)
        => await _db.StaffMembers
            .Where(s => s.UserId == userId)
            .Join(_db.Users.Where(u => u.PropertyId == propertyId),
                  s => s.UserId, u => u.UserId, (s, _) => s)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<StaffMember>> GetAvailableAsync(int propertyId, CancellationToken ct)
        => await _db.StaffMembers
            .Where(s => s.Availability == StaffState.AVAILABLE)
            .Join(_db.Users.Where(u => u.PropertyId == propertyId && u.IsActive),
                  s => s.UserId, u => u.UserId, (s, _) => s)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StaffMember>> GetOnDutyAsync(int propertyId, CancellationToken ct)
        => await _db.StaffMembers
            .Where(s => s.Availability != StaffState.OFF_DUTY)
            .Join(_db.Users.Where(u => u.PropertyId == propertyId && u.IsActive),
                  s => s.UserId, u => u.UserId, (s, _) => s)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StaffMember>> GetAllByPropertyAsync(int propertyId, CancellationToken ct)
        => await _db.StaffMembers
            .Join(_db.Users.Where(u => u.PropertyId == propertyId),
                  s => s.UserId, u => u.UserId, (s, _) => s)
            .ToListAsync(ct);

    public async Task AddAsync(StaffMember staffMember, CancellationToken ct)
    {
        await _db.StaffMembers.AddAsync(staffMember, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(StaffMember staffMember, CancellationToken ct)
    {
        _db.StaffMembers.Update(staffMember);
        await _db.SaveChangesAsync(ct);
    }
}
