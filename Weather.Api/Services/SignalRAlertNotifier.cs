using Microsoft.AspNetCore.SignalR;
using Weather.Api.Hubs;
using Weather.Domain.Abstractions;
using Weather.Domain.Contracts.Alerts;

namespace Weather.Api.Services;

public sealed class SignalRAlertNotifier(IHubContext<AlertsHub> hubContext) : IAlertNotifier
{
    public Task NotifyTriggeredAsync(
        string email,
        TriggeredAlertResponse alert,
        CancellationToken cancellationToken = default)
        => hubContext.Clients
            .Group(AlertChannel.ForEmail(email))
            .SendAsync(AlertsHub.ClientMethod, alert, cancellationToken);
}


