using MediatR;
using Weather.Domain.Contracts.Alerts;

namespace Weather.Application.Alerts.Queries.GetSubscriptions;

public sealed record GetSubscriptionsQuery(string Email) : IRequest<IReadOnlyList<AlertSubscriptionResponse>>;


