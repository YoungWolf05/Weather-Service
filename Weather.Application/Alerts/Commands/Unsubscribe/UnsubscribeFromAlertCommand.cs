using MediatR;

namespace Weather.Application.Alerts.Commands.Unsubscribe;

/// <param name="SubscriptionId">ID of the subscription to deactivate.</param>
/// <param name="Email">Must match the subscription's email — acts as ownership proof.</param>
public sealed record UnsubscribeFromAlertCommand(
    int SubscriptionId,
    string Email) : IRequest;
