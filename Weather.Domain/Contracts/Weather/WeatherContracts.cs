namespace Weather.Domain.Contracts.Weather;

public sealed record LocationResponse(
    int Id,
    string Name,
    string Type,
    string? ExternalId,
    string? Region,
    decimal? Latitude,
    decimal? Longitude);

public sealed record WeatherObservationResponse(
    decimal TemperatureCelsius,
    DateTime ObservedAt);

public sealed record WeatherForecastResponse(
    string Condition,
    decimal? TempLowCelsius,
    decimal? TempHighCelsius,
    decimal? HumidityLowPct,
    decimal? HumidityHighPct,
    string? WindDirection,
    decimal? WindSpeedLowKmh,
    decimal? WindSpeedHighKmh,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    DateTime? IssuedAt);

public sealed record CurrentWeatherResponse(
    string Location,
    string Type,
    string? ExternalId,
    string? Region,
    WeatherObservationResponse? Observation,
    WeatherForecastResponse? Forecast);

public sealed record ForecastPeriodResponse(
    DateTime ValidFrom,
    DateTime ValidTo,
    string Forecast);

public sealed record ForecastResponse(
    string Region,
    DateTime IssuedAt,
    WeatherForecastResponse? General,
    IReadOnlyList<ForecastPeriodResponse> Periods);

public sealed record HistoricalObservationResponse(
    decimal TemperatureCelsius,
    DateTime ObservedAt);

public sealed record HistoricalForecastResponse(
    DateTime IssuedAt,
    DateTime ValidFrom,
    DateTime ValidTo,
    string ForecastType,
    string Forecast);

public sealed record HistoricalWeatherResponse(
    string Location,
    DateTime From,
    DateTime To,
    int Count,
    IReadOnlyList<HistoricalObservationResponse>? Observations,
    IReadOnlyList<HistoricalForecastResponse>? Forecasts);


