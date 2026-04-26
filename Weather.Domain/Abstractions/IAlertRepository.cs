using Weather.Domain.Contracts.Alerts;
using Weather.Domain.Entities;

namespace Weather.Domain.Abstractions;

public interface IAlertRepository
{
    Task<IReadOnlyList<AlertSubscription>> GetActiveSubscriptionsAsync(CancellationToken cancellationToken = default);

    Task AddSubscriptionAsync(AlertSubscription subscription, CancellationToken cancellationToken = default);

    Task DeleteSubscriptionAsync(int id, string email, CancellationToken cancellationToken = default);

    Task AddTriggeredAlertAsync(TriggeredAlert alert, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AlertSubscriptionResponse>> GetSubscriptionsForEmailAsync(string email, CancellationToken cancellationToken = default);
}
