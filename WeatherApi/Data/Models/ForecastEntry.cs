namespace WeatherApi.Data.Models;

public class ForecastEntry
{
    public int Id { get; set; }
    public int LocationId { get; set; }
    public Location Location { get; set; } = null!;
    public DateTime IssuedAt { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public string ForecastType { get; set; } = null!;   // "general" | "period"
    public string ForecastText { get; set; } = null!;
    // General-only fields (null for period entries)
    public decimal? TempLowCelsius { get; set; }
    public decimal? TempHighCelsius { get; set; }
    public decimal? HumidityLowPct { get; set; }
    public decimal? HumidityHighPct { get; set; }
    public string? WindDirection { get; set; }
    public decimal? WindSpeedLowKmh { get; set; }
    public decimal? WindSpeedHighKmh { get; set; }
    public DateTime CreatedAt { get; set; }
}
