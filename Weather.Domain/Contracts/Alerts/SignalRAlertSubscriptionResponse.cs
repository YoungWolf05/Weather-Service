namespace Weather.Domain.Contracts.Alerts;

public sealed record SignalRAlertSubscriptionResponse(
    int Id,
    string Email,
    string LocationName,
    decimal ThresholdCelsius,
    string Condition,
    bool IsActive,
    DateTime CreatedAt,
    string LiveChannel);


