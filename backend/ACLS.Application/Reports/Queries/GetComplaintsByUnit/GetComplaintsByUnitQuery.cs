using ACLS.Application.Common.Interfaces;
using ACLS.Application.Complaints.DTOs;
using ACLS.Domain.Complaints;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Reports.Queries.GetComplaintsByUnit;

public sealed record GetComplaintsByUnitQuery(int UnitId) : IRequest<Result<IReadOnlyList<ComplaintSummaryDto>>>;

public sealed class GetComplaintsByUnitQueryHandler
    : IRequestHandler<GetComplaintsByUnitQuery, Result<IReadOnlyList<ComplaintSummaryDto>>>
{
    private readonly IComplaintReadService _readService;
    private readonly ICurrentPropertyContext _propertyContext;

    public GetComplaintsByUnitQueryHandler(
        IComplaintReadService readService,
        ICurrentPropertyContext propertyContext)
    {
        _readService = readService;
        _propertyContext = propertyContext;
    }

    public async Task<Result<IReadOnlyList<ComplaintSummaryDto>>> Handle(
        GetComplaintsByUnitQuery query,
        CancellationToken cancellationToken)
    {
        var options = new ComplaintQueryOptions(UnitId: query.UnitId, PageSize: 200);
        var (items, _) = await _readService.GetEnrichedAsync(
            _propertyContext.PropertyId, options, cancellationToken);
        return Result<IReadOnlyList<ComplaintSummaryDto>>.Success(items);
    }
}
