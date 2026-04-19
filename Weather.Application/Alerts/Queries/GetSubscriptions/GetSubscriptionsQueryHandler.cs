using MediatR;
using Weather.Application.Abstractions;
using Weather.Application.Contracts;

namespace Weather.Application.Alerts.Queries.GetSubscriptions;

public sealed class GetSubscriptionsQueryHandler(
    IAlertRepository alertRepository) : IRequestHandler<GetSubscriptionsQuery, IReadOnlyList<AlertSubscriptionResponse>>
{
    public Task<IReadOnlyList<AlertSubscriptionResponse>> Handle(
        GetSubscriptionsQuery request,
        CancellationToken cancellationToken)
        => alertRepository.GetSubscriptionsForEmailAsync(request.Email.ToLowerInvariant(), cancellationToken);
}
