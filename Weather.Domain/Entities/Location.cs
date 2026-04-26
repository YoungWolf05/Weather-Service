namespace Weather.Domain.Entities;

public class Location
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string? ExternalId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Region { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<WeatherObservation> Observations { get; set; } = [];
    public ICollection<ForecastEntry> Forecasts { get; set; } = [];
    public ICollection<AlertSubscription> AlertSubscriptions { get; set; } = [];
}


