using Weather.Application.Contracts;

namespace Weather.Application.Abstractions;

public interface IWeatherQueryService
{
    Task<IReadOnlyList<LocationResponse>> GetLocationsAsync(string? type, CancellationToken cancellationToken = default);
    Task<CurrentWeatherResponse> GetCurrentAsync(string location, CancellationToken cancellationToken = default);
    Task<ForecastResponse> GetForecastAsync(string location, CancellationToken cancellationToken = default);
    Task<HistoricalWeatherResponse> GetHistoricalAsync(
        string location,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}
