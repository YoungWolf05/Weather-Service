using Microsoft.AspNetCore.SignalR;

namespace Weather.Api.Hubs;

public sealed class AlertsHub : Hub
{
    public const string ClientMethod = "alertTriggered";

    public override async Task OnConnectedAsync()
    {
        var email = Context.GetHttpContext()?.Request.Query["email"].ToString().Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email))
            throw new HubException("The 'email' query string is required.");

        await Groups.AddToGroupAsync(Context.ConnectionId, AlertChannel.ForEmail(email));
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var email = Context.GetHttpContext()?.Request.Query["email"].ToString().Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(email))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, AlertChannel.ForEmail(email));

        await base.OnDisconnectedAsync(exception);
    }
}

public static class AlertChannel
{
    public static string ForEmail(string email)
        => $"alerts:{email.Trim().ToLowerInvariant()}";
}
