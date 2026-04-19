namespace Weather.Application.Contracts;

public sealed record AlertSubscriptionResponse(
    int Id,
    string Email,
    string LocationName,
    decimal ThresholdCelsius,
    string Condition,
    bool IsActive,
    DateTime CreatedAt);

public sealed record TriggeredAlertResponse(
    long Id,
    string LocationName,
    decimal TemperatureCelsius,
    decimal ThresholdCelsius,
    string Condition,
    DateTime TriggeredAt);
