using MediatR;
using Weather.Application.Contracts;

namespace Weather.Application.Alerts.Commands.Subscribe;

/// <param name="Email">Subscriber's email address.</param>
/// <param name="Location">Station name or external ID (stations only — regions are not supported).</param>
/// <param name="ThresholdCelsius">Temperature value that triggers the alert.</param>
/// <param name="Condition">"above" or "below".</param>
public sealed record SubscribeToAlertCommand(
    string Email,
    string Location,
    decimal ThresholdCelsius,
    string Condition) : IRequest<AlertSubscriptionResponse>;
