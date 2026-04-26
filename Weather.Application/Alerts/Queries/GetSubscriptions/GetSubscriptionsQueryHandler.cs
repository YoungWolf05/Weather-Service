using MediatR;
using Weather.Domain.Abstractions;
using Weather.Domain.Contracts.Alerts;

namespace Weather.Application.Alerts.Queries.GetSubscriptions;

public sealed class GetSubscriptionsQueryHandler(
    IAlertRepository alertRepository) : IRequestHandler<GetSubscriptionsQuery, IReadOnlyList<AlertSubscriptionResponse>>
{
    public Task<IReadOnlyList<AlertSubscriptionResponse>> Handle(
        GetSubscriptionsQuery request,
        CancellationToken cancellationToken)
        => alertRepository.GetSubscriptionsForEmailAsync(request.Email.ToLowerInvariant(), cancellationToken);
}


