using MediatR;
using Weather.Application.Abstractions;
using Weather.Application.Contracts;
using Weather.Domain.Entities;

namespace Weather.Application.Alerts.Commands.EvaluateAlerts;

public sealed class EvaluateAlertsCommandHandler(
    IAlertRepository alertRepository,
    IWeatherReadRepository weatherRepository,
    IAlertNotifier alertNotifier) : IRequestHandler<EvaluateAlertsCommand, int>
{
    public async Task<int> Handle(EvaluateAlertsCommand request, CancellationToken cancellationToken)
    {
        var subscriptions = await alertRepository.GetActiveSubscriptionsAsync(cancellationToken);
        var fired = 0;

        foreach (var subscription in subscriptions)
        {
            var latest = await weatherRepository.GetLatestObservationAsync(subscription.LocationId, cancellationToken);

            if (latest is null)
                continue;

            var (observationId, temperatureCelsius, _) = latest.Value;

            var conditionMet = subscription.Condition == "above"
                ? temperatureCelsius > subscription.ThresholdCelsius
                : temperatureCelsius < subscription.ThresholdCelsius;

            if (!conditionMet)
                continue;

            var alreadyTriggered = await alertRepository.HasAlertBeenTriggeredAsync(
                subscription.Id, observationId, cancellationToken);

            if (alreadyTriggered)
                continue;

            var triggeredAlert = new TriggeredAlert
            {
                AlertSubscriptionId = subscription.Id,
                ObservationId       = observationId,
                TemperatureCelsius  = temperatureCelsius,
                TriggeredAt         = DateTime.UtcNow
            };

            await alertRepository.AddTriggeredAlertAsync(triggeredAlert, cancellationToken);

            await alertNotifier.NotifyTriggeredAsync(
                subscription.Email,
                new TriggeredAlertResponse(
                    triggeredAlert.Id,
                    subscription.Location.Name,
                    triggeredAlert.TemperatureCelsius,
                    subscription.ThresholdCelsius,
                    subscription.Condition,
                    triggeredAlert.TriggeredAt),
                cancellationToken);

            fired++;
        }

        return fired;
    }
}
