using ACLS.Application.Complaints.DTOs;
using ACLS.Domain.Complaints;

namespace ACLS.Application.Common.Interfaces;

/// <summary>
/// Read-only query service for Complaints that returns enriched view-model DTOs via joined
/// projections. Separated from IComplaintRepository (which works with domain aggregates) to
/// avoid pulling application-layer types into the domain.
/// </summary>
public interface IComplaintReadService
{
    /// <summary>
    /// Returns a paginated, filtered list of complaints enriched with display names from
    /// related tables (resident name, unit number, building name, assigned staff name).
    /// </summary>
    Task<(IReadOnlyList<ComplaintSummaryDto> Items, int TotalCount)> GetEnrichedAsync(
        int propertyId,
        ComplaintQueryOptions? options,
        CancellationToken ct);

    /// <summary>
    /// Returns a single enriched ComplaintDto with UnitNumber, BuildingName, ResidentName and
    /// AssignedStaffMember populated. Returns null if not found or not in the property.
    /// </summary>
    Task<ComplaintDto?> GetEnrichedByIdAsync(
        int complaintId,
        int propertyId,
        CancellationToken ct);
}
