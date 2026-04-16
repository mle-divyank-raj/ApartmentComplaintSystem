using ACLS.Application.Common.Interfaces;
using ACLS.Application.Complaints.DTOs;
using ACLS.Domain.Complaints;
using Microsoft.EntityFrameworkCore;

namespace ACLS.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IComplaintReadService.
/// Uses a single joined projection to return display-name–enriched complaint summaries
/// without loading full domain aggregates.
/// </summary>
public sealed class ComplaintReadService : IComplaintReadService
{
    private readonly AclsDbContext _db;

    public ComplaintReadService(AclsDbContext db) => _db = db;

    public async Task<(IReadOnlyList<ComplaintSummaryDto> Items, int TotalCount)> GetEnrichedAsync(
        int propertyId,
        ComplaintQueryOptions? options,
        CancellationToken ct)
    {
        var query = _db.Complaints
            .Where(c => c.PropertyId == propertyId)
            .Join(_db.Residents,
                c => c.ResidentId,
                r => r.ResidentId,
                (c, r) => new { Complaint = c, Resident = r })
            .Join(_db.Users,
                cr => cr.Resident.UserId,
                u => u.UserId,
                (cr, u) => new { cr.Complaint, ResidentUser = u })
            .Join(_db.Units,
                cr => cr.Complaint.UnitId,
                unit => unit.UnitId,
                (cr, unit) => new { cr.Complaint, cr.ResidentUser, Unit = unit })
            .Join(_db.Buildings,
                cru => cru.Unit.BuildingId,
                b => b.BuildingId,
                (cru, b) => new { cru.Complaint, cru.ResidentUser, cru.Unit, Building = b })
            .GroupJoin(_db.StaffMembers,
                crub => crub.Complaint.AssignedStaffMemberId,
                sm => sm.StaffMemberId,
                (crub, staffMembers) => new { crub, staffMembers })
            .SelectMany(
                x => x.staffMembers.DefaultIfEmpty(),
                (x, sm) => new { x.crub.Complaint, x.crub.ResidentUser, x.crub.Unit, x.crub.Building, StaffMember = sm })
            .GroupJoin(_db.Users,
                x => x.StaffMember != null ? x.StaffMember.UserId : (int?)null,
                u => u.UserId,
                (x, staffUsers) => new { x, staffUsers })
            .SelectMany(
                x => x.staffUsers.DefaultIfEmpty(),
                (x, staffUser) => new
                {
                    x.x.Complaint,
                    x.x.ResidentUser,
                    x.x.Unit,
                    x.x.Building,
                    x.x.StaffMember,
                    StaffUser = staffUser
                })
            .AsQueryable();

        // ── Filters ──────────────────────────────────────────────────────────

        if (options is not null)
        {
            if (options.Status.HasValue)
                query = query.Where(x => x.Complaint.Status == options.Status.Value);

            if (options.Urgency.HasValue)
                query = query.Where(x => x.Complaint.Urgency == options.Urgency.Value);

            if (!string.IsNullOrWhiteSpace(options.Category))
                query = query.Where(x => x.Complaint.Category == options.Category);

            if (options.DateFrom.HasValue)
                query = query.Where(x => x.Complaint.CreatedAt >= options.DateFrom.Value);

            if (options.DateTo.HasValue)
                query = query.Where(x => x.Complaint.CreatedAt <= options.DateTo.Value);

            if (!string.IsNullOrWhiteSpace(options.Search))
                query = query.Where(x =>
                    x.Complaint.Title.Contains(options.Search) ||
                    x.Complaint.Description.Contains(options.Search));

            if (options.ResidentId.HasValue)
                query = query.Where(x => x.Complaint.ResidentId == options.ResidentId.Value);

            if (options.UnitId.HasValue)
                query = query.Where(x => x.Complaint.UnitId == options.UnitId.Value);
        }

        var totalCount = await query.CountAsync(ct);

        // ── Sort ─────────────────────────────────────────────────────────────

        query = options?.SortBy?.ToLowerInvariant() switch
        {
            "createdat" when options.SortDirection?.ToLowerInvariant() == "asc"
                => query.OrderBy(x => x.Complaint.CreatedAt),
            _ => query.OrderByDescending(x => x.Complaint.CreatedAt)
        };

        // ── Pagination ────────────────────────────────────────────────────────

        if (options is not null)
        {
            var page = Math.Max(1, options.Page);
            var pageSize = Math.Clamp(options.PageSize, 1, 100);
            query = query.Skip((page - 1) * pageSize).Take(pageSize);
        }

        // ── Project ───────────────────────────────────────────────────────────

        var items = await query
            .Select(x => new ComplaintSummaryDto(
                x.Complaint.ComplaintId,
                x.Complaint.UnitId,
                x.Unit.UnitNumber,
                x.Building.Name,
                x.Complaint.ResidentId,
                x.ResidentUser.FirstName + " " + x.ResidentUser.LastName,
                x.Complaint.AssignedStaffMemberId,
                x.StaffUser != null ? x.StaffUser.FirstName + " " + x.StaffUser.LastName : null,
                x.Complaint.Title,
                x.Complaint.Category,
                x.Complaint.Urgency.ToString(),
                x.Complaint.Status.ToString(),
                x.Complaint.CreatedAt,
                x.Complaint.UpdatedAt,
                x.Complaint.ResolvedAt))
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<ComplaintDto?> GetEnrichedByIdAsync(
        int complaintId,
        int propertyId,
        CancellationToken ct)
    {
        // Load complaint with navigation properties (WorkNotes, Media)
        var complaint = await _db.Complaints
            .Include(c => c.WorkNotes)
            .Include(c => c.Media)
            .FirstOrDefaultAsync(c => c.ComplaintId == complaintId && c.PropertyId == propertyId, ct);

        if (complaint is null)
            return null;

        // Fetch enriched display names in a single projection
        var enriched = await _db.Complaints
            .Where(c => c.ComplaintId == complaintId)
            .Join(_db.Residents, c => c.ResidentId, r => r.ResidentId, (c, r) => new { Resident = r })
            .Join(_db.Users, x => x.Resident.UserId, u => u.UserId, (x, u) => new { ResidentUser = u })
            .Select(x => new { ResidentName = x.ResidentUser.FirstName + " " + x.ResidentUser.LastName })
            .FirstOrDefaultAsync(ct);

        var unitInfo = await _db.Units
            .Where(u => u.UnitId == complaint.UnitId)
            .Join(_db.Buildings, u => u.BuildingId, b => b.BuildingId,
                (u, b) => new { u.UnitNumber, BuildingName = b.Name })
            .FirstOrDefaultAsync(ct);

        AssignedStaffMemberDto? staffDto = null;
        if (complaint.AssignedStaffMemberId.HasValue)
        {
            staffDto = await _db.StaffMembers
                .Where(sm => sm.StaffMemberId == complaint.AssignedStaffMemberId.Value)
                .Join(_db.Users, sm => sm.UserId, u => u.UserId,
                    (sm, u) => new AssignedStaffMemberDto(
                        sm.StaffMemberId,
                        u.FirstName + " " + u.LastName,
                        sm.JobTitle,
                        sm.Availability.ToString()))
                .FirstOrDefaultAsync(ct);
        }

        // Enrich work notes with staff member display names
        var workNoteStaffIds = complaint.WorkNotes.Select(w => w.StaffMemberId).Distinct().ToList();
        var staffNameById = await _db.StaffMembers
            .Where(sm => workNoteStaffIds.Contains(sm.StaffMemberId))
            .Join(_db.Users, sm => sm.UserId, u => u.UserId,
                (sm, u) => new { sm.StaffMemberId, FullName = u.FirstName + " " + u.LastName })
            .ToDictionaryAsync(x => x.StaffMemberId, x => x.FullName, ct);

        var enrichedWorkNotes = complaint.WorkNotes
            .Select(w => WorkNoteDto.FromDomain(w, staffNameById.GetValueOrDefault(w.StaffMemberId, string.Empty)))
            .ToList();

        return ComplaintDto.FromDomain(complaint) with
        {
            UnitNumber    = unitInfo?.UnitNumber    ?? string.Empty,
            BuildingName  = unitInfo?.BuildingName  ?? string.Empty,
            ResidentName  = enriched?.ResidentName  ?? string.Empty,
            AssignedStaffMember = staffDto,
            WorkNotes = enrichedWorkNotes,
        };
    }
}
