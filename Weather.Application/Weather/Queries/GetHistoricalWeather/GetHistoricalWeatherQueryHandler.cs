using MediatR;
using Weather.Application.Abstractions;
using Weather.Application.Contracts;

namespace Weather.Application.Weather.Queries.GetHistoricalWeather;

public sealed class GetHistoricalWeatherQueryHandler(IWeatherReadRepository repository)
    : IRequestHandler<GetHistoricalWeatherQuery, HistoricalWeatherResponse>
{
    public Task<HistoricalWeatherResponse> Handle(
        GetHistoricalWeatherQuery request,
        CancellationToken cancellationToken)
        => repository.GetHistoricalWeatherAsync(
            request.Location,
            request.From,
            request.To,
            cancellationToken);
}
