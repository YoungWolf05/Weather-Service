using Microsoft.EntityFrameworkCore;
using Weather.Domain.Abstractions;
using Weather.Domain.Contracts.Alerts;
using Weather.Domain.Entities;
using Weather.Infrastructure.Persistence;

namespace Weather.Infrastructure.Repositories;

public class AlertRepository(WeatherDbContext dbContext) : IAlertRepository
{
    public async Task<IReadOnlyList<AlertSubscription>> GetActiveSubscriptionsAsync(
        CancellationToken cancellationToken = default)
        => await dbContext.AlertSubscriptions
            .Include(s => s.Location)
            .ToListAsync(cancellationToken);

    public async Task AddSubscriptionAsync(
        AlertSubscription subscription,
        CancellationToken cancellationToken = default)
    {
        dbContext.AlertSubscriptions.Add(subscription);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteSubscriptionAsync(
        int id,
        string email,
        CancellationToken cancellationToken = default)
    {
        var subscription = await dbContext.AlertSubscriptions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Subscription {id} not found.");

        if (!subscription.Email.Equals(email.ToLowerInvariant(), StringComparison.Ordinal))
            throw new ArgumentException("Email does not match the subscription owner.", nameof(email));

        dbContext.AlertSubscriptions.Remove(subscription);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

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
}
