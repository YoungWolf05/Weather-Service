namespace Weather.Domain.Entities;

public class WeatherObservation
{
    public long Id { get; set; }
    public int LocationId { get; set; }
    public Location Location { get; set; } = null!;
    public DateTime ObservedAt { get; set; }
    public decimal TemperatureCelsius { get; set; }
    public DateTime CreatedAt { get; set; }
}


