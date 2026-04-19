using Microsoft.AspNetCore.SignalR;
using Weather.Api.Hubs;
using Weather.Application.Abstractions;
using Weather.Application.Contracts;

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
