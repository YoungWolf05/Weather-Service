using CsvHelper.Configuration;
using Weather.Domain.Contracts.Weather;

namespace Weather.Api.Csv;

/// <summary>
/// Maps <see cref="CurrentWeatherExportDto"/> properties to human-readable CSV column headers.
/// Keeping this in the API project ensures CsvHelper stays a presentation-layer dependency.
/// </summary>
public sealed class WeatherExportMap : ClassMap<CurrentWeatherExportDto>
{
    public WeatherExportMap()
    {
        Map(m => m.Location).Name("Location");
        Map(m => m.Type).Name("Type");
        Map(m => m.ExternalId).Name("Station ID");
        Map(m => m.Region).Name("Region");
        Map(m => m.ObservedAt).Name("Observed At (UTC)");
        Map(m => m.TemperatureCelsius).Name("Temperature (°C)");
        Map(m => m.ForecastCondition).Name("Forecast Condition");
        Map(m => m.TempLowCelsius).Name("Temp Low (°C)");
        Map(m => m.TempHighCelsius).Name("Temp High (°C)");
        Map(m => m.HumidityLowPct).Name("Humidity Low (%)");
        Map(m => m.HumidityHighPct).Name("Humidity High (%)");
        Map(m => m.WindDirection).Name("Wind Direction");
        Map(m => m.WindSpeedLowKmh).Name("Wind Low (km/h)");
        Map(m => m.WindSpeedHighKmh).Name("Wind High (km/h)");
        Map(m => m.ForecastValidFrom).Name("Forecast Valid From (UTC)");
        Map(m => m.ForecastValidTo).Name("Forecast Valid To (UTC)");
        Map(m => m.ForecastIssuedAt).Name("Forecast Issued At (UTC)");
    }
}


