using ACLS.Application.Common.Interfaces;
using ACLS.Application.Complaints.DTOs;
using ACLS.Domain.Complaints;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Complaints.Queries.GetComplaintsByResident;

public sealed record GetComplaintsByResidentQuery(int ResidentId)
    : IRequest<Result<IReadOnlyList<ComplaintSummaryDto>>>;

public sealed class GetComplaintsByResidentQueryHandler
    : IRequestHandler<GetComplaintsByResidentQuery, Result<IReadOnlyList<ComplaintSummaryDto>>>
{
    private readonly IComplaintRepository _complaintRepository;
    private readonly ICurrentPropertyContext _propertyContext;

    public GetComplaintsByResidentQueryHandler(
        IComplaintRepository complaintRepository,
        ICurrentPropertyContext propertyContext)
    {
        _complaintRepository = complaintRepository;
        _propertyContext = propertyContext;
    }

    public async Task<Result<IReadOnlyList<ComplaintSummaryDto>>> Handle(
        GetComplaintsByResidentQuery query,
        CancellationToken cancellationToken)
    {
        var complaints = await _complaintRepository.GetByResidentAsync(
            query.ResidentId,
            _propertyContext.PropertyId,
            cancellationToken);

        var dtos = complaints.Select(ComplaintSummaryDto.FromDomain).ToList();
        return Result<IReadOnlyList<ComplaintSummaryDto>>.Success(dtos);
    }
}
