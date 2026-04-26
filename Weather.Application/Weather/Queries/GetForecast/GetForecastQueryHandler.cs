using MediatR;
using Weather.Domain.Abstractions;
using Weather.Domain.Contracts.Weather;

namespace Weather.Application.Weather.Queries.GetForecast;

public sealed class GetForecastQueryHandler(IWeatherReadRepository repository)
    : IRequestHandler<GetForecastQuery, ForecastResponse>
{
    public Task<ForecastResponse> Handle(
        GetForecastQuery request,
        CancellationToken cancellationToken)
        => repository.GetForecastAsync(request.Location, cancellationToken);
}


