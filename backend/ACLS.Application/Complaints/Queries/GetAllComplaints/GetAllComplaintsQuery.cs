using ACLS.Application.Common;
using ACLS.Application.Common.Interfaces;
using ACLS.Application.Complaints.DTOs;
using ACLS.Domain.Complaints;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Complaints.Queries.GetAllComplaints;

public sealed record GetAllComplaintsQuery(
    ComplaintQueryOptions? Options = null) : IRequest<Result<PagedResult<ComplaintSummaryDto>>>;

public sealed class GetAllComplaintsQueryHandler
    : IRequestHandler<GetAllComplaintsQuery, Result<PagedResult<ComplaintSummaryDto>>>
{
    private readonly IComplaintReadService _readService;
    private readonly ICurrentPropertyContext _propertyContext;

    public GetAllComplaintsQueryHandler(
        IComplaintReadService readService,
        ICurrentPropertyContext propertyContext)
    {
        _readService = readService;
        _propertyContext = propertyContext;
    }

    public async Task<Result<PagedResult<ComplaintSummaryDto>>> Handle(
        GetAllComplaintsQuery query,
        CancellationToken cancellationToken)
    {
        var page     = Math.Max(1, query.Options?.Page ?? 1);
        var pageSize = Math.Clamp(query.Options?.PageSize ?? 20, 1, 100);

        var (items, totalCount) = await _readService.GetEnrichedAsync(
            _propertyContext.PropertyId,
            query.Options,
            cancellationToken);

        return Result<PagedResult<ComplaintSummaryDto>>.Success(
            new PagedResult<ComplaintSummaryDto>(items, totalCount, page, pageSize));
    }
}
