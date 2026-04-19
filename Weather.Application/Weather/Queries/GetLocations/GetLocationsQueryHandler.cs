using MediatR;
using Weather.Application.Abstractions;
using Weather.Application.Contracts;

namespace Weather.Application.Weather.Queries.GetLocations;

public sealed class GetLocationsQueryHandler(IWeatherReadRepository repository)
    : IRequestHandler<GetLocationsQuery, IReadOnlyList<LocationResponse>>
{
    public Task<IReadOnlyList<LocationResponse>> Handle(
        GetLocationsQuery request,
        CancellationToken cancellationToken)
        => repository.GetLocationsAsync(request.Type, cancellationToken);
}
