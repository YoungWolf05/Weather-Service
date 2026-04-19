namespace Weather.Application.Contracts;

/// <summary>
/// Flat projection of <see cref="CurrentWeatherResponse"/> used for CSV export.
/// All nested objects are inlined so each row is self-contained.
/// </summary>
public sealed record CurrentWeatherExportDto(
    string   Location,
    string   Type,
    string?  ExternalId,
    string?  Region,
    // Observation (stations only)
    DateTime? ObservedAt,
    decimal?  TemperatureCelsius,
    // Forecast (regions / stations with a mapped region)
    string?  ForecastCondition,
    decimal? TempLowCelsius,
    decimal? TempHighCelsius,
    decimal? HumidityLowPct,
    decimal? HumidityHighPct,
    string?  WindDirection,
    decimal? WindSpeedLowKmh,
    decimal? WindSpeedHighKmh,
    DateTime? ForecastValidFrom,
    DateTime? ForecastValidTo,
    DateTime? ForecastIssuedAt);
