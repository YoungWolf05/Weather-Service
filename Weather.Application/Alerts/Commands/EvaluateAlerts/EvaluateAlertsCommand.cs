using MediatR;

namespace Weather.Application.Alerts.Commands.EvaluateAlerts;

/// <summary>
/// Evaluates all active alert subscriptions against the latest observation at each subscribed station.
/// Creates a <c>TriggeredAlert</c> for any subscription whose threshold condition is met for an
/// observation that has not already been processed.
/// </summary>
/// <returns>Number of new alerts fired.</returns>
public sealed record EvaluateAlertsCommand : IRequest<int>;
