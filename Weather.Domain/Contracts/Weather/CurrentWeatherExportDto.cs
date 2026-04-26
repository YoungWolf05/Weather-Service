namespace Weather.Domain.Contracts.Weather;

public sealed record CurrentWeatherExportDto(
    string Location,
    string Type,
    string? ExternalId,
    string? Region,
    DateTime? ObservedAt,
    decimal? TemperatureCelsius,
    string? ForecastCondition,
    decimal? TempLowCelsius,
    decimal? TempHighCelsius,
    decimal? HumidityLowPct,
    decimal? HumidityHighPct,
    string? WindDirection,
    decimal? WindSpeedLowKmh,
    decimal? WindSpeedHighKmh,
    DateTime? ForecastValidFrom,
    DateTime? ForecastValidTo,
    DateTime? ForecastIssuedAt);


