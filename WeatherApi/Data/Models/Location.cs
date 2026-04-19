namespace WeatherApi.Data.Models;

public class Location
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;       // "station" | "region"
    public string? ExternalId { get; set; }          // e.g. "S108" for stations
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Region { get; set; }              // which of the 5 regions this station belongs to
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<WeatherObservation> Observations { get; set; } = [];
    public ICollection<ForecastEntry> Forecasts { get; set; } = [];
}
