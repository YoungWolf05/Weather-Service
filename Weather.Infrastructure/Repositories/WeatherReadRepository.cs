using Microsoft.EntityFrameworkCore;
using Weather.Application.Abstractions;
using Weather.Application.Contracts;
using Weather.Domain.Entities;
using Weather.Infrastructure.Persistence;

namespace Weather.Infrastructure.Repositories;

public class WeatherReadRepository(WeatherDbContext dbContext) : IWeatherReadRepository
{
    public async Task<IReadOnlyList<LocationResponse>> GetLocationsAsync(
        string? type,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Locations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(l => l.Type == type);

        return await query
            .OrderBy(l => l.Type)
            .ThenBy(l => l.Name)
            .Select(l => new LocationResponse(
                l.Id, l.Name, l.Type, l.ExternalId, l.Region, l.Latitude, l.Longitude))
            .ToListAsync(cancellationToken);
    }

    public async Task<CurrentWeatherResponse> GetCurrentWeatherAsync(
        string location,
        CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveLocationAsync(location, cancellationToken)
            ?? throw new KeyNotFoundException($"Location '{location}' not found.");

        WeatherObservationResponse? observation = null;
        WeatherForecastResponse? forecast = null;

        if (resolved.Type == "station")
        {
            observation = await dbContext.WeatherObservations
                .Where(o => o.LocationId == resolved.Id)
                .OrderByDescending(o => o.ObservedAt)
                .Select(o => new WeatherObservationResponse(o.TemperatureCelsius, o.ObservedAt))
                .FirstOrDefaultAsync(cancellationToken);
        }

        var regionName = resolved.Type == "region"
            ? resolved.Region ?? "singapore"
            : resolved.Region;

        if (regionName is not null)
        {
            var now = DateTime.UtcNow;

            var latestIssued = await dbContext.ForecastEntries
                .Where(f => f.ForecastType == "period" && f.ValidFrom <= now && f.ValidTo >= now)
                .MaxAsync(f => (DateTime?)f.IssuedAt, cancellationToken);

            if (latestIssued.HasValue)
            {
                var regionLocation = await dbContext.Locations
                    .FirstOrDefaultAsync(l => l.Name == regionName, cancellationToken);
                var singaporeLocation = await dbContext.Locations
                    .FirstOrDefaultAsync(l => l.Name == "singapore", cancellationToken);

                ForecastEntry? period = null;
                ForecastEntry? general = null;

                if (regionLocation is not null)
                {
                    period = await dbContext.ForecastEntries.FirstOrDefaultAsync(
                        f => f.LocationId == regionLocation.Id
                          && f.ForecastType == "period"
                          && f.IssuedAt == latestIssued
                          && f.ValidFrom <= now
                          && f.ValidTo >= now,
                        cancellationToken);
                }

                if (singaporeLocation is not null)
                {
                    general = await dbContext.ForecastEntries.FirstOrDefaultAsync(
                        f => f.LocationId == singaporeLocation.Id
                          && f.ForecastType == "general"
                          && f.IssuedAt == latestIssued,
                        cancellationToken);
                }

                if (period is not null || general is not null)
                {
                    forecast = new WeatherForecastResponse(
                        period?.ForecastText ?? general?.ForecastText ?? string.Empty,
                        general?.TempLowCelsius,
                        general?.TempHighCelsius,
                        general?.HumidityLowPct,
                        general?.HumidityHighPct,
                        general?.WindDirection,
                        general?.WindSpeedLowKmh,
                        general?.WindSpeedHighKmh,
                        period?.ValidFrom ?? general?.ValidFrom,
                        period?.ValidTo ?? general?.ValidTo,
                        period?.IssuedAt ?? general?.IssuedAt);
                }
            }
        }

        return new CurrentWeatherResponse(
            resolved.Name,
            resolved.Type,
            resolved.ExternalId,
            resolved.Region,
            observation,
            forecast);
    }

    public async Task<ForecastResponse> GetForecastAsync(
        string location,
        CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveLocationAsync(location, cancellationToken)
            ?? throw new KeyNotFoundException($"Location '{location}' not found.");

        var regionName = resolved.Type == "region"
            ? resolved.Region ?? "singapore"
            : resolved.Region;

        if (regionName is null)
            throw new ArgumentException("Provide a region name (north/south/east/west/central).", nameof(location));

        var regionLocation = await dbContext.Locations
            .FirstOrDefaultAsync(l => l.Name == regionName, cancellationToken)
            ?? throw new KeyNotFoundException($"Region '{regionName}' not found.");

        var singaporeLocation = await dbContext.Locations
            .FirstOrDefaultAsync(l => l.Name == "singapore", cancellationToken);

        var latestIssued = await dbContext.ForecastEntries
            .Where(f => f.LocationId == regionLocation.Id)
            .MaxAsync(f => (DateTime?)f.IssuedAt, cancellationToken);

        if (latestIssued is null)
            throw new ArgumentException("No forecast data available yet. Try again after seeding.", nameof(location));

        WeatherForecastResponse? general = null;

        if (singaporeLocation is not null)
        {
            var generalEntry = await dbContext.ForecastEntries.FirstOrDefaultAsync(
                f => f.LocationId == singaporeLocation.Id
                  && f.ForecastType == "general"
                  && f.IssuedAt == latestIssued,
                cancellationToken);

            if (generalEntry is not null)
            {
                general = new WeatherForecastResponse(
                    generalEntry.ForecastText,
                    generalEntry.TempLowCelsius,
                    generalEntry.TempHighCelsius,
                    generalEntry.HumidityLowPct,
                    generalEntry.HumidityHighPct,
                    generalEntry.WindDirection,
                    generalEntry.WindSpeedLowKmh,
                    generalEntry.WindSpeedHighKmh,
                    generalEntry.ValidFrom,
                    generalEntry.ValidTo,
                    generalEntry.IssuedAt);
            }
        }

        var periods = await dbContext.ForecastEntries
            .Where(f => f.LocationId == regionLocation.Id
                && f.ForecastType == "period"
                && f.IssuedAt == latestIssued)
            .OrderBy(f => f.ValidFrom)
            .Select(f => new ForecastPeriodResponse(f.ValidFrom, f.ValidTo, f.ForecastText))
            .ToListAsync(cancellationToken);

        return new ForecastResponse(regionName, latestIssued.Value, general, periods);
    }

    public async Task<HistoricalWeatherResponse> GetHistoricalWeatherAsync(
        string location,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        if (to < from)
            throw new ArgumentException("'to' must be after 'from'.", nameof(to));

        var resolved = await ResolveLocationAsync(location, cancellationToken)
            ?? throw new KeyNotFoundException($"Location '{location}' not found.");

        var utcFrom = DateTime.SpecifyKind(from, DateTimeKind.Utc);
        var utcTo = DateTime.SpecifyKind(to, DateTimeKind.Utc);

        if (resolved.Type == "station")
        {
            var observations = await dbContext.WeatherObservations
                .Where(o => o.LocationId == resolved.Id
                    && o.ObservedAt >= utcFrom
                    && o.ObservedAt <= utcTo)
                .OrderBy(o => o.ObservedAt)
                .Select(o => new HistoricalObservationResponse(o.TemperatureCelsius, o.ObservedAt))
                .ToListAsync(cancellationToken);

            return new HistoricalWeatherResponse(
                resolved.Name, utcFrom, utcTo, observations.Count, observations, null);
        }

        var regionName = resolved.Region ?? resolved.Name;
        var regionLocation = await dbContext.Locations
            .FirstOrDefaultAsync(l => l.Name == regionName, cancellationToken) ?? resolved;

        var forecasts = await dbContext.ForecastEntries
            .Where(f => f.LocationId == regionLocation.Id
                && f.ValidFrom >= utcFrom
                && f.ValidFrom <= utcTo)
            .OrderBy(f => f.IssuedAt)
            .ThenBy(f => f.ValidFrom)
            .Select(f => new HistoricalForecastResponse(
                f.IssuedAt, f.ValidFrom, f.ValidTo, f.ForecastType, f.ForecastText))
            .ToListAsync(cancellationToken);

        return new HistoricalWeatherResponse(
            resolved.Name, utcFrom, utcTo, forecasts.Count, null, forecasts);
    }

    private Task<Location?> ResolveLocationAsync(string location, CancellationToken cancellationToken)
    {
        var lower = location.ToLower();
        return dbContext.Locations.FirstOrDefaultAsync(
            l => l.Name.ToLower() == lower || l.ExternalId == location,
            cancellationToken);
    }
}
