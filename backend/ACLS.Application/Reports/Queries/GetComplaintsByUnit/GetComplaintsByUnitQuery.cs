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
    private readonly IComplaintRepository _complaintRepository;
    private readonly ICurrentPropertyContext _propertyContext;

    public GetComplaintsByUnitQueryHandler(
        IComplaintRepository complaintRepository,
        ICurrentPropertyContext propertyContext)
    {
        _complaintRepository = complaintRepository;
        _propertyContext = propertyContext;
    }

    public async Task<Result<IReadOnlyList<ComplaintSummaryDto>>> Handle(
        GetComplaintsByUnitQuery query,
        CancellationToken cancellationToken)
    {
        var complaints = await _complaintRepository.GetByUnitAsync(
            query.UnitId, _propertyContext.PropertyId, cancellationToken);

        var dtos = complaints.Select(ComplaintSummaryDto.FromDomain).ToList();
        return Result<IReadOnlyList<ComplaintSummaryDto>>.Success(dtos);
    }
}
