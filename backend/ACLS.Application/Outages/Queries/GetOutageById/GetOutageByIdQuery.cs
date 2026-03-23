using ACLS.Application.Common.Interfaces;
using ACLS.Application.Outages.DTOs;
using ACLS.Domain.Outages;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Outages.Queries.GetOutageById;

public sealed record GetOutageByIdQuery(int OutageId) : IRequest<Result<OutageDto>>;

public sealed class GetOutageByIdQueryHandler
    : IRequestHandler<GetOutageByIdQuery, Result<OutageDto>>
{
    private readonly IOutageRepository _outageRepository;
    private readonly ICurrentPropertyContext _propertyContext;

    public GetOutageByIdQueryHandler(
        IOutageRepository outageRepository,
        ICurrentPropertyContext propertyContext)
    {
        _outageRepository = outageRepository;
        _propertyContext = propertyContext;
    }

    public async Task<Result<OutageDto>> Handle(
        GetOutageByIdQuery query,
        CancellationToken cancellationToken)
    {
        var outage = await _outageRepository.GetByIdAsync(
            query.OutageId, _propertyContext.PropertyId, cancellationToken);

        if (outage is null)
            return Result<OutageDto>.Failure(
                new Error("Outage.NotFound", $"Outage with ID {query.OutageId} was not found."));

        return Result<OutageDto>.Success(OutageDto.FromDomain(outage));
    }
}
