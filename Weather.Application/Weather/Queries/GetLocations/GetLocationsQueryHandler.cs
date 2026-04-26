using MediatR;
using Weather.Domain.Abstractions;
using Weather.Domain.Contracts.Weather;

namespace Weather.Application.Weather.Queries.GetLocations;

public sealed class GetLocationsQueryHandler(IWeatherReadRepository repository)
    : IRequestHandler<GetLocationsQuery, IReadOnlyList<LocationResponse>>
{
    public Task<IReadOnlyList<LocationResponse>> Handle(
        GetLocationsQuery request,
        CancellationToken cancellationToken)
        => repository.GetLocationsAsync(request.Type, cancellationToken);
}


