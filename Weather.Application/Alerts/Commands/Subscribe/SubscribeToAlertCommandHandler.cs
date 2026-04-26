using MediatR;
using Weather.Domain.Abstractions;
using Weather.Domain.Contracts.Alerts;
using Weather.Domain.Entities;

namespace Weather.Application.Alerts.Commands.Subscribe;

public sealed class SubscribeToAlertCommandHandler(
    IAlertRepository alertRepository,
    IWeatherReadRepository weatherRepository) : IRequestHandler<SubscribeToAlertCommand, SignalRAlertSubscriptionResponse>
{
    private static readonly HashSet<string> ValidConditions = ["above", "below"];

    public async Task<SignalRAlertSubscriptionResponse> Handle(
        SubscribeToAlertCommand request,
        CancellationToken cancellationToken)
    {
        var condition = request.Condition.ToLowerInvariant();

        if (!ValidConditions.Contains(condition))
            throw new ArgumentException($"Condition must be 'above' or 'below', got '{request.Condition}'.", nameof(request.Condition));

        var locations = await weatherRepository.GetLocationsAsync(type: null, cancellationToken);
        var lower = request.Location.ToLowerInvariant();

        var location = locations.FirstOrDefault(l =>
            l.Name.Equals(lower, StringComparison.OrdinalIgnoreCase) ||
            (l.ExternalId is not null && l.ExternalId.Equals(request.Location, StringComparison.OrdinalIgnoreCase)))
            ?? throw new KeyNotFoundException($"Location '{request.Location}' not found.");

        if (!location.Type.Equals("station", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"Alerts can only be set on stations, not on '{location.Type}' locations like '{location.Name}'.",
                nameof(request.Location));

        var subscription = new AlertSubscription
        {
            Email            = request.Email.ToLowerInvariant(),
            LocationId       = location.Id,
            ThresholdCelsius = request.ThresholdCelsius,
            Condition        = condition,
            IsActive         = true,
            CreatedAt        = DateTime.UtcNow
        };

        await alertRepository.AddSubscriptionAsync(subscription, cancellationToken);

        return new SignalRAlertSubscriptionResponse(
            subscription.Id,
            subscription.Email,
            location.Name,
            subscription.ThresholdCelsius,
            subscription.Condition,
            subscription.IsActive,
            subscription.CreatedAt,
            $"alerts:{subscription.Email}");
    }
}
