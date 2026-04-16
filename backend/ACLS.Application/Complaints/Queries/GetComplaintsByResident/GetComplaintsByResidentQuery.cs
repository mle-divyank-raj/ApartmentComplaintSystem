using ACLS.Application.Common.Interfaces;
using ACLS.Application.Complaints.DTOs;
using ACLS.Domain.Complaints;
using ACLS.Domain.Residents;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Complaints.Queries.GetComplaintsByResident;

public sealed record GetComplaintsByResidentQuery(int ResidentId)
    : IRequest<Result<IReadOnlyList<ComplaintSummaryDto>>>;

public sealed class GetComplaintsByResidentQueryHandler
    : IRequestHandler<GetComplaintsByResidentQuery, Result<IReadOnlyList<ComplaintSummaryDto>>>
{
    private readonly IComplaintReadService _readService;
    private readonly ICurrentPropertyContext _propertyContext;
    private readonly IResidentRepository _residentRepository;

    public GetComplaintsByResidentQueryHandler(
        IComplaintReadService readService,
        ICurrentPropertyContext propertyContext,
        IResidentRepository residentRepository)
    {
        _readService = readService;
        _propertyContext = propertyContext;
        _residentRepository = residentRepository;
    }

    public async Task<Result<IReadOnlyList<ComplaintSummaryDto>>> Handle(
        GetComplaintsByResidentQuery query,
        CancellationToken cancellationToken)
    {
        // Resolve the actual ResidentId from the logged-in user when not explicitly provided
        var residentId = query.ResidentId;
        if (residentId == 0)
        {
            var resident = await _residentRepository.GetByUserIdAsync(
                _propertyContext.UserId, _propertyContext.PropertyId, cancellationToken);
            residentId = resident?.ResidentId ?? 0;
        }

        var options = new ComplaintQueryOptions(ResidentId: residentId, PageSize: 200);
        var (items, _) = await _readService.GetEnrichedAsync(
            _propertyContext.PropertyId, options, cancellationToken);
        return Result<IReadOnlyList<ComplaintSummaryDto>>.Success(items);
    }
}
