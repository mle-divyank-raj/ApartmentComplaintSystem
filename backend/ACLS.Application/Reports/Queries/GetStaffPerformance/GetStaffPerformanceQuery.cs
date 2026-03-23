using ACLS.Application.Common.Interfaces;
using ACLS.Application.Reports.DTOs;
using ACLS.Domain.Reporting;
using ACLS.Domain.Staff;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Reports.Queries.GetStaffPerformance;

public sealed record GetStaffPerformanceQuery : IRequest<Result<IReadOnlyList<StaffPerformanceSummaryDto>>>;

public sealed class GetStaffPerformanceQueryHandler
    : IRequestHandler<GetStaffPerformanceQuery, Result<IReadOnlyList<StaffPerformanceSummaryDto>>>
{
    private readonly IReportingService _reportingService;
    private readonly IStaffRepository _staffRepository;
    private readonly ICurrentPropertyContext _propertyContext;

    public GetStaffPerformanceQueryHandler(
        IReportingService reportingService,
        IStaffRepository staffRepository,
        ICurrentPropertyContext propertyContext)
    {
        _reportingService = reportingService;
        _staffRepository = staffRepository;
        _propertyContext = propertyContext;
    }

    public async Task<Result<IReadOnlyList<StaffPerformanceSummaryDto>>> Handle(
        GetStaffPerformanceQuery query,
        CancellationToken cancellationToken)
    {
        // Default: last 30 days
        var toUtc = DateTime.UtcNow;
        var fromUtc = toUtc.AddDays(-30);

        var summaries = await _reportingService.GetStaffPerformanceAsync(
            _propertyContext.PropertyId, fromUtc, toUtc, cancellationToken);

        var allStaff = await _staffRepository.GetAllByPropertyAsync(
            _propertyContext.PropertyId, cancellationToken);

        var staffById = allStaff.ToDictionary(s => s.StaffMemberId);

        var dtos = summaries
            .Select(s =>
            {
                var jobTitle = staffById.TryGetValue(s.StaffMemberId, out var staff) ? staff.JobTitle : null;
                return StaffPerformanceSummaryDto.FromDomain(s, jobTitle);
            })
            .ToList();

        return Result<IReadOnlyList<StaffPerformanceSummaryDto>>.Success(dtos);
    }
}
