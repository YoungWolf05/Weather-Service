using MediatR;
using Weather.Application.Abstractions;
using Weather.Application.Contracts;

namespace Weather.Application.Weather.Queries.ExportCurrentWeather;

public sealed class ExportCurrentWeatherQueryHandler(IWeatherReadRepository repository)
    : IRequestHandler<ExportCurrentWeatherQuery, CurrentWeatherExportDto>
{
    public async Task<CurrentWeatherExportDto> Handle(
        ExportCurrentWeatherQuery request,
        CancellationToken cancellationToken)
    {
        var weather = await repository.GetCurrentWeatherAsync(request.Location, cancellationToken);

        return new CurrentWeatherExportDto(
            weather.Location,
            weather.Type,
            weather.ExternalId,
            weather.Region,
            weather.Observation?.ObservedAt,
            weather.Observation?.TemperatureCelsius,
            weather.Forecast?.Condition,
            weather.Forecast?.TempLowCelsius,
            weather.Forecast?.TempHighCelsius,
            weather.Forecast?.HumidityLowPct,
            weather.Forecast?.HumidityHighPct,
            weather.Forecast?.WindDirection,
            weather.Forecast?.WindSpeedLowKmh,
            weather.Forecast?.WindSpeedHighKmh,
            weather.Forecast?.ValidFrom,
            weather.Forecast?.ValidTo,
            weather.Forecast?.IssuedAt);
    }
}
