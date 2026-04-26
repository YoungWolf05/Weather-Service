using MediatR;
using Weather.Domain.Abstractions;
using Weather.Domain.Contracts.Weather;

namespace Weather.Application.Weather.Queries.GetCurrentWeather;

public sealed class GetCurrentWeatherQueryHandler(IWeatherReadRepository repository)
    : IRequestHandler<GetCurrentWeatherQuery, CurrentWeatherResponse>
{
    public Task<CurrentWeatherResponse> Handle(
        GetCurrentWeatherQuery request,
        CancellationToken cancellationToken)
        => repository.GetCurrentWeatherAsync(request.Location, cancellationToken);
}


