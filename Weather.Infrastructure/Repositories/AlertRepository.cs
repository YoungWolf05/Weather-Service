using Microsoft.EntityFrameworkCore;
using Weather.Application.Abstractions;
using Weather.Application.Contracts;
using Weather.Domain.Entities;
using Weather.Infrastructure.Persistence;

namespace Weather.Infrastructure.Repositories;

public class AlertRepository(WeatherDbContext dbContext) : IAlertRepository
{
    public async Task<IReadOnlyList<AlertSubscription>> GetActiveSubscriptionsAsync(
        CancellationToken cancellationToken = default)
        => await dbContext.AlertSubscriptions
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddSubscriptionAsync(
        AlertSubscription subscription,
        CancellationToken cancellationToken = default)
    {
        dbContext.AlertSubscriptions.Add(subscription);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateSubscriptionAsync(
        int id,
        string email,
        CancellationToken cancellationToken = default)
    {
        var subscription = await dbContext.AlertSubscriptions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Subscription {id} not found.");

        if (!subscription.Email.Equals(email.ToLowerInvariant(), StringComparison.Ordinal))
            throw new ArgumentException("Email does not match the subscription owner.", nameof(email));

        subscription.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> HasAlertBeenTriggeredAsync(
        int subscriptionId,
        long observationId,
        CancellationToken cancellationToken = default)
        => dbContext.TriggeredAlerts
            .AnyAsync(
                t => t.AlertSubscriptionId == subscriptionId && t.ObservationId == observationId,
                cancellationToken);

    public async Task AddTriggeredAlertAsync(
        TriggeredAlert alert,
        CancellationToken cancellationToken = default)
    {
        dbContext.TriggeredAlerts.Add(alert);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AlertSubscriptionResponse>> GetSubscriptionsForEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
        => await dbContext.AlertSubscriptions
            .Where(s => s.Email == email)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new AlertSubscriptionResponse(
                s.Id,
                s.Email,
                s.Location.Name,
                s.ThresholdCelsius,
                s.Condition,
                s.IsActive,
                s.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TriggeredAlertResponse>> GetTriggeredAlertsForEmailAsync(
        string email,
        int limit,
        CancellationToken cancellationToken = default)
        => await dbContext.TriggeredAlerts
            .Where(t => t.AlertSubscription.Email == email)
            .OrderByDescending(t => t.TriggeredAt)
            .Take(limit)
            .Select(t => new TriggeredAlertResponse(
                t.Id,
                t.AlertSubscription.Location.Name,
                t.TemperatureCelsius,
                t.AlertSubscription.ThresholdCelsius,
                t.AlertSubscription.Condition,
                t.TriggeredAt))
            .ToListAsync(cancellationToken);
}
