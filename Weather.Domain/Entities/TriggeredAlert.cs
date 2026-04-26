namespace Weather.Domain.Entities;

public class TriggeredAlert
{
    public long Id { get; set; }
    public int AlertSubscriptionId { get; set; }
    public AlertSubscription AlertSubscription { get; set; } = null!;
    public long ObservationId { get; set; }
    public WeatherObservation Observation { get; set; } = null!;
    public decimal TemperatureCelsius { get; set; }
    public DateTime TriggeredAt { get; set; }
}


