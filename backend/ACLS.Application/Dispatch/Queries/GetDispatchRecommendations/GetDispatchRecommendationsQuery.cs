using ACLS.Application.Common.Interfaces;
using ACLS.Application.Dispatch.DTOs;
using ACLS.Domain.Complaints;
using ACLS.Domain.Dispatch;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Dispatch.Queries.GetDispatchRecommendations;

public sealed record GetDispatchRecommendationsQuery(int ComplaintId)
    : IRequest<Result<IReadOnlyList<StaffScoreDto>>>;

/// <summary>
/// Returns ranked staff recommendations for a given Complaint using IDispatchService.
/// The actual assignment is a separate AssignComplaintCommand — this query is read-only.
/// </summary>
public sealed class GetDispatchRecommendationsQueryHandler
    : IRequestHandler<GetDispatchRecommendationsQuery, Result<IReadOnlyList<StaffScoreDto>>>
{
    private readonly IComplaintRepository _complaintRepository;
    private readonly IDispatchService _dispatchService;
    private readonly ICurrentPropertyContext _propertyContext;

    public GetDispatchRecommendationsQueryHandler(
        IComplaintRepository complaintRepository,
        IDispatchService dispatchService,
        ICurrentPropertyContext propertyContext)
    {
        _complaintRepository = complaintRepository;
        _dispatchService = dispatchService;
        _propertyContext = propertyContext;
    }

    public async Task<Result<IReadOnlyList<StaffScoreDto>>> Handle(
        GetDispatchRecommendationsQuery query,
        CancellationToken cancellationToken)
    {
        var complaint = await _complaintRepository.GetByIdAsync(
            query.ComplaintId,
            _propertyContext.PropertyId,
            cancellationToken);

        if (complaint is null)
            return Result<IReadOnlyList<StaffScoreDto>>.Failure(
                ComplaintErrors.NotFound(query.ComplaintId));

        var scores = await _dispatchService.FindOptimalStaffAsync(complaint, cancellationToken);

        var dtos = scores.Select(StaffScoreDto.FromDomain).ToList();
        return Result<IReadOnlyList<StaffScoreDto>>.Success(dtos);
    }
}
