using Weather.Application.Contracts;
using Weather.Domain.Entities;

namespace Weather.Application.Abstractions;

public interface IAlertRepository
{
    Task<IReadOnlyList<AlertSubscription>> GetActiveSubscriptionsAsync(CancellationToken cancellationToken = default);

    Task AddSubscriptionAsync(AlertSubscription subscription, CancellationToken cancellationToken = default);

    Task DeactivateSubscriptionAsync(int id, string email, CancellationToken cancellationToken = default);

    Task<bool> HasAlertBeenTriggeredAsync(int subscriptionId, long observationId, CancellationToken cancellationToken = default);

    Task AddTriggeredAlertAsync(TriggeredAlert alert, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AlertSubscriptionResponse>> GetSubscriptionsForEmailAsync(string email, CancellationToken cancellationToken = default);
}
