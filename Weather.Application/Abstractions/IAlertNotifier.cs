using Weather.Application.Contracts;

namespace Weather.Application.Abstractions;

public interface IAlertNotifier
{
    Task NotifyTriggeredAsync(string email, TriggeredAlertResponse alert, CancellationToken cancellationToken = default);
}
