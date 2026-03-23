using Microsoft.EntityFrameworkCore;
using ACLS.Domain.Complaints;

namespace ACLS.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IComplaintRepository.
/// EVERY query applies a PropertyId filter as the first WHERE clause — this is the
/// primary multi-tenancy enforcement mechanism in the persistence layer.
/// </summary>
public sealed class ComplaintRepository : IComplaintRepository
{
    private readonly AclsDbContext _db;

    public ComplaintRepository(AclsDbContext db) => _db = db;

    public async Task<Complaint?> GetByIdAsync(int complaintId, int propertyId, CancellationToken ct)
        => await _db.Complaints
            .Where(c => c.PropertyId == propertyId && c.ComplaintId == complaintId)
            .Include(c => c.Media)
            .Include(c => c.WorkNotes)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Complaint>> GetAllAsync(
        int propertyId,
        ComplaintQueryOptions? options,
        CancellationToken ct)
    {
        var query = _db.Complaints
            .Where(c => c.PropertyId == propertyId)
            .AsQueryable();

        if (options is not null)
        {
            if (options.Status.HasValue)
                query = query.Where(c => c.Status == options.Status.Value);

            if (options.Urgency.HasValue)
                query = query.Where(c => c.Urgency == options.Urgency.Value);

            if (!string.IsNullOrWhiteSpace(options.Category))
                query = query.Where(c => c.Category == options.Category);

            if (options.DateFrom.HasValue)
                query = query.Where(c => c.CreatedAt >= options.DateFrom.Value);

            if (options.DateTo.HasValue)
                query = query.Where(c => c.CreatedAt <= options.DateTo.Value);

            if (!string.IsNullOrWhiteSpace(options.Search))
                query = query.Where(c =>
                    c.Title.Contains(options.Search) ||
                    c.Description.Contains(options.Search));

            query = (options.SortBy?.ToLowerInvariant(), options.SortDirection?.ToLowerInvariant()) switch
            {
                ("createdat", "asc")  => query.OrderBy(c => c.CreatedAt),
                ("createdat", _)      => query.OrderByDescending(c => c.CreatedAt),
                ("urgency", _)        => query.OrderByDescending(c => c.Urgency),
                _                     => query.OrderByDescending(c => c.CreatedAt)
            };

            var page     = Math.Max(1, options.Page);
            var pageSize = Math.Clamp(options.PageSize, 1, 100);
            query = query.Skip((page - 1) * pageSize).Take(pageSize);
        }

        return await query.ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Complaint>> GetByResidentAsync(
        int residentId, int propertyId, CancellationToken ct)
        => await _db.Complaints
            .Where(c => c.PropertyId == propertyId && c.ResidentId == residentId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Complaint>> GetByStaffMemberAsync(
        int staffMemberId, int propertyId, CancellationToken ct)
        => await _db.Complaints
            .Where(c => c.PropertyId == propertyId && c.AssignedStaffMemberId == staffMemberId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Complaint>> GetByUnitAsync(
        int unitId, int propertyId, CancellationToken ct)
        => await _db.Complaints
            .Where(c => c.PropertyId == propertyId && c.UnitId == unitId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Complaint complaint, CancellationToken ct)
    {
        await _db.Complaints.AddAsync(complaint, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Complaint complaint, CancellationToken ct)
    {
        _db.Complaints.Update(complaint);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddMediaAsync(Media media, CancellationToken ct)
    {
        await _db.Media.AddAsync(media, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddWorkNoteAsync(WorkNote workNote, CancellationToken ct)
    {
        await _db.WorkNotes.AddAsync(workNote, ct);
        await _db.SaveChangesAsync(ct);
    }
}
