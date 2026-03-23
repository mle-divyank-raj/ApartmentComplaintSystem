using ACLS.Application.Common.Interfaces;
using ACLS.Application.Outages.DTOs;
using ACLS.Domain.Outages;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Outages.Queries.GetAllOutages;

public sealed record GetAllOutagesQuery : IRequest<Result<IReadOnlyList<OutageDto>>>;

public sealed class GetAllOutagesQueryHandler
    : IRequestHandler<GetAllOutagesQuery, Result<IReadOnlyList<OutageDto>>>
{
    private readonly IOutageRepository _outageRepository;
    private readonly ICurrentPropertyContext _propertyContext;

    public GetAllOutagesQueryHandler(
        IOutageRepository outageRepository,
        ICurrentPropertyContext propertyContext)
    {
        _outageRepository = outageRepository;
        _propertyContext = propertyContext;
    }

    public async Task<Result<IReadOnlyList<OutageDto>>> Handle(
        GetAllOutagesQuery query,
        CancellationToken cancellationToken)
    {
        var outages = await _outageRepository.GetAllByPropertyAsync(
            _propertyContext.PropertyId, cancellationToken);

        var dtos = outages.Select(OutageDto.FromDomain).ToList();
        return Result<IReadOnlyList<OutageDto>>.Success(dtos);
    }
}
