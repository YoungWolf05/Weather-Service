using MediatR;
using Weather.Application.Contracts;

namespace Weather.Application.Alerts.Queries.GetSubscriptions;

public sealed record GetSubscriptionsQuery(string Email) : IRequest<IReadOnlyList<AlertSubscriptionResponse>>;
