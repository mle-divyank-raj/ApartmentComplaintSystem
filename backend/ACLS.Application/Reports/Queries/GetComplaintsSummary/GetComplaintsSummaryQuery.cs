using ACLS.Application.Common.Interfaces;
using ACLS.Application.Complaints.DTOs;
using ACLS.Domain.Complaints;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Reports.Queries.GetComplaintsSummary;

public sealed record GetComplaintsSummaryQuery(
    ComplaintQueryOptions? Options = null) : IRequest<Result<IReadOnlyList<ComplaintSummaryDto>>>;

public sealed class GetComplaintsSummaryQueryHandler
    : IRequestHandler<GetComplaintsSummaryQuery, Result<IReadOnlyList<ComplaintSummaryDto>>>
{
    private readonly IComplaintReadService _readService;
    private readonly ICurrentPropertyContext _propertyContext;

    public GetComplaintsSummaryQueryHandler(
        IComplaintReadService readService,
        ICurrentPropertyContext propertyContext)
    {
        _readService = readService;
        _propertyContext = propertyContext;
    }

    public async Task<Result<IReadOnlyList<ComplaintSummaryDto>>> Handle(
        GetComplaintsSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var (items, _) = await _readService.GetEnrichedAsync(
            _propertyContext.PropertyId, query.Options, cancellationToken);
        return Result<IReadOnlyList<ComplaintSummaryDto>>.Success(items);
    }
}
