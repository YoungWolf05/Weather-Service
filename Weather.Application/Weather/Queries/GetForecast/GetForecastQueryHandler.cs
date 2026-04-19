using MediatR;
using Weather.Application.Abstractions;
using Weather.Application.Contracts;

namespace Weather.Application.Weather.Queries.GetForecast;

public sealed class GetForecastQueryHandler(IWeatherReadRepository repository)
    : IRequestHandler<GetForecastQuery, ForecastResponse>
{
    public Task<ForecastResponse> Handle(
        GetForecastQuery request,
        CancellationToken cancellationToken)
        => repository.GetForecastAsync(request.Location, cancellationToken);
}
