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
    private readonly IComplaintReadService _readService;
    private readonly ICurrentPropertyContext _propertyContext;

    public GetComplaintByIdQueryHandler(
        IComplaintReadService readService,
        ICurrentPropertyContext propertyContext)
    {
        _readService = readService;
        _propertyContext = propertyContext;
    }

    public async Task<Result<ComplaintDto>> Handle(
        GetComplaintByIdQuery query,
        CancellationToken cancellationToken)
    {
        var dto = await _readService.GetEnrichedByIdAsync(
            query.ComplaintId,
            _propertyContext.PropertyId,
            cancellationToken);

        if (dto is null)
            return Result<ComplaintDto>.Failure(ComplaintErrors.NotFound(query.ComplaintId));

        return Result<ComplaintDto>.Success(dto);
    }
}
