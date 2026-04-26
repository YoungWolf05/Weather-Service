using Weather.Domain.Contracts.Weather;

namespace Weather.Domain.Abstractions;

public interface IWeatherReadRepository
{
    Task<IReadOnlyList<LocationResponse>> GetLocationsAsync(
        string? type,
        CancellationToken cancellationToken = default);

    Task<CurrentWeatherResponse> GetCurrentWeatherAsync(
        string location,
        CancellationToken cancellationToken = default);

    Task<ForecastResponse> GetForecastAsync(
        string location,
        CancellationToken cancellationToken = default);

    Task<HistoricalWeatherResponse> GetHistoricalWeatherAsync(
        string location,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<(long Id, decimal TemperatureCelsius, DateTime ObservedAt)?> GetLatestObservationAsync(
        int locationId,
        CancellationToken cancellationToken = default);
}


