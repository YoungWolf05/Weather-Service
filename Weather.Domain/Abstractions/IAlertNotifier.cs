using Weather.Domain.Contracts.Alerts;

namespace Weather.Domain.Abstractions;

public interface IAlertNotifier
{
    Task NotifyTriggeredAsync(string email, TriggeredAlertResponse alert, CancellationToken cancellationToken = default);
}


