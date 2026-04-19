namespace Weather.Domain.Entities;

public class AlertSubscription
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public int LocationId { get; set; }
    public Location Location { get; set; } = null!;
    public decimal ThresholdCelsius { get; set; }

    /// <summary>"above" or "below"</summary>
    public string Condition { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public ICollection<TriggeredAlert> TriggeredAlerts { get; set; } = [];
}
