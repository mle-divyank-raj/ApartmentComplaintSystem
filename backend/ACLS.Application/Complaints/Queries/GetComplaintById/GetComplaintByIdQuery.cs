using ACLS.Application.Common.Interfaces;
using ACLS.Application.Complaints.DTOs;
using ACLS.Domain.Complaints;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Complaints.Queries.GetComplaintById;

public sealed record GetComplaintByIdQuery(int ComplaintId) : IRequest<Result<ComplaintDto>>;

public sealed class GetComplaintByIdQueryHandler
    : IRequestHandler<GetComplaintByIdQuery, Result<ComplaintDto>>
{
    private readonly IComplaintRepository _complaintRepository;
    private readonly ICurrentPropertyContext _propertyContext;

    public GetComplaintByIdQueryHandler(
        IComplaintRepository complaintRepository,
        ICurrentPropertyContext propertyContext)
    {
        _complaintRepository = complaintRepository;
        _propertyContext = propertyContext;
    }

    public async Task<Result<ComplaintDto>> Handle(
        GetComplaintByIdQuery query,
        CancellationToken cancellationToken)
    {
        var complaint = await _complaintRepository.GetByIdAsync(
            query.ComplaintId,
            _propertyContext.PropertyId,
            cancellationToken);

        if (complaint is null)
            return Result<ComplaintDto>.Failure(ComplaintErrors.NotFound(query.ComplaintId));

        return Result<ComplaintDto>.Success(ComplaintDto.FromDomain(complaint));
    }
}
