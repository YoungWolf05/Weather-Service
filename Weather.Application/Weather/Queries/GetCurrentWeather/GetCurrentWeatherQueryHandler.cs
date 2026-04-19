using MediatR;
using Weather.Application.Abstractions;
using Weather.Application.Contracts;

namespace Weather.Application.Weather.Queries.GetCurrentWeather;

public sealed class GetCurrentWeatherQueryHandler(IWeatherReadRepository repository)
    : IRequestHandler<GetCurrentWeatherQuery, CurrentWeatherResponse>
{
    public Task<CurrentWeatherResponse> Handle(
        GetCurrentWeatherQuery request,
        CancellationToken cancellationToken)
        => repository.GetCurrentWeatherAsync(request.Location, cancellationToken);
}
