using ACLS.Application.Common.Interfaces;
using ACLS.Application.Complaints.DTOs;
using ACLS.Domain.Complaints;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Complaints.Queries.GetAllComplaints;

public sealed record GetAllComplaintsQuery(
    ComplaintQueryOptions? Options = null) : IRequest<Result<IReadOnlyList<ComplaintSummaryDto>>>;

public sealed class GetAllComplaintsQueryHandler
    : IRequestHandler<GetAllComplaintsQuery, Result<IReadOnlyList<ComplaintSummaryDto>>>
{
    private readonly IComplaintRepository _complaintRepository;
    private readonly ICurrentPropertyContext _propertyContext;

    public GetAllComplaintsQueryHandler(
        IComplaintRepository complaintRepository,
        ICurrentPropertyContext propertyContext)
    {
        _complaintRepository = complaintRepository;
        _propertyContext = propertyContext;
    }

    public async Task<Result<IReadOnlyList<ComplaintSummaryDto>>> Handle(
        GetAllComplaintsQuery query,
        CancellationToken cancellationToken)
    {
        var complaints = await _complaintRepository.GetAllAsync(
            _propertyContext.PropertyId,
            query.Options,
            cancellationToken);

        var dtos = complaints.Select(ComplaintSummaryDto.FromDomain).ToList();
        return Result<IReadOnlyList<ComplaintSummaryDto>>.Success(dtos);
    }
}
