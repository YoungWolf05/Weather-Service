using Microsoft.EntityFrameworkCore;
using Weather.Application.Abstractions;
using Weather.Application.Contracts;
using Weather.Domain.Entities;
using Weather.Infrastructure.Persistence;

namespace Weather.Infrastructure.Services;

public class WeatherQueryService(WeatherDbContext dbContext) : IWeatherQueryService
{
    public async Task<IReadOnlyList<LocationResponse>> GetLocationsAsync(
        string? type,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Locations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(location => location.Type == type);
        }

        return await query
            .OrderBy(location => location.Type)
            .ThenBy(location => location.Name)
            .Select(location => new LocationResponse(
                location.Id,
                location.Name,
                location.Type,
                location.ExternalId,
                location.Region,
                location.Latitude,
                location.Longitude))
            .ToListAsync(cancellationToken);
    }

    public async Task<CurrentWeatherResponse> GetCurrentAsync(
        string location,
        CancellationToken cancellationToken = default)
    {
        var resolvedLocation = await ResolveLocationAsync(location, cancellationToken)
            ?? throw new KeyNotFoundException($"Location '{location}' not found.");

        WeatherObservationResponse? observation = null;
        WeatherForecastResponse? forecast = null;

        if (resolvedLocation.Type == "station")
        {
            observation = await dbContext.WeatherObservations
                .Where(item => item.LocationId == resolvedLocation.Id)
                .OrderByDescending(item => item.ObservedAt)
                .Select(item => new WeatherObservationResponse(item.TemperatureCelsius, item.ObservedAt))
                .FirstOrDefaultAsync(cancellationToken);
        }

        var regionName = resolvedLocation.Type == "region"
            ? resolvedLocation.Region ?? "singapore"
            : resolvedLocation.Region;

        if (regionName is not null)
        {
            var now = DateTime.UtcNow;

            var latestIssued = await dbContext.ForecastEntries
                .Where(item => item.ForecastType == "period" && item.ValidFrom <= now && item.ValidTo >= now)
                .MaxAsync(item => (DateTime?)item.IssuedAt, cancellationToken);

            if (latestIssued.HasValue)
            {
                var regionLocation = await dbContext.Locations
                    .FirstOrDefaultAsync(item => item.Name == regionName, cancellationToken);
                var singaporeLocation = await dbContext.Locations
                    .FirstOrDefaultAsync(item => item.Name == "singapore", cancellationToken);

                ForecastEntry? period = null;
                ForecastEntry? general = null;

                if (regionLocation is not null)
                {
                    period = await dbContext.ForecastEntries.FirstOrDefaultAsync(
                        item => item.LocationId == regionLocation.Id
                            && item.ForecastType == "period"
                            && item.IssuedAt == latestIssued
                            && item.ValidFrom <= now
                            && item.ValidTo >= now,
                        cancellationToken);
                }

                if (singaporeLocation is not null)
                {
                    general = await dbContext.ForecastEntries.FirstOrDefaultAsync(
                        item => item.LocationId == singaporeLocation.Id
                            && item.ForecastType == "general"
                            && item.IssuedAt == latestIssued,
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
            resolvedLocation.Name,
            resolvedLocation.Type,
            resolvedLocation.ExternalId,
            resolvedLocation.Region,
            observation,
            forecast);
    }

    public async Task<ForecastResponse> GetForecastAsync(
        string location,
        CancellationToken cancellationToken = default)
    {
        var resolvedLocation = await ResolveLocationAsync(location, cancellationToken)
            ?? throw new KeyNotFoundException($"Location '{location}' not found.");

        var regionName = resolvedLocation.Type == "region"
            ? resolvedLocation.Region ?? "singapore"
            : resolvedLocation.Region;

        if (regionName is null)
        {
            throw new ArgumentException("Provide a region name (north/south/east/west/central).", nameof(location));
        }

        var regionLocation = await dbContext.Locations
            .FirstOrDefaultAsync(item => item.Name == regionName, cancellationToken)
            ?? throw new KeyNotFoundException($"Region '{regionName}' not found.");

        var singaporeLocation = await dbContext.Locations
            .FirstOrDefaultAsync(item => item.Name == "singapore", cancellationToken);

        var latestIssued = await dbContext.ForecastEntries
            .Where(item => item.LocationId == regionLocation.Id)
            .MaxAsync(item => (DateTime?)item.IssuedAt, cancellationToken);

        if (latestIssued is null)
        {
            throw new ArgumentException("No forecast data available yet. Try again after seeding.", nameof(location));
        }

        WeatherForecastResponse? general = null;

        if (singaporeLocation is not null)
        {
            var generalEntry = await dbContext.ForecastEntries.FirstOrDefaultAsync(
                item => item.LocationId == singaporeLocation.Id
                    && item.ForecastType == "general"
                    && item.IssuedAt == latestIssued,
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
            .Where(item => item.LocationId == regionLocation.Id
                && item.ForecastType == "period"
                && item.IssuedAt == latestIssued)
            .OrderBy(item => item.ValidFrom)
            .Select(item => new ForecastPeriodResponse(item.ValidFrom, item.ValidTo, item.ForecastText))
            .ToListAsync(cancellationToken);

        return new ForecastResponse(regionName, latestIssued.Value, general, periods);
    }

    public async Task<HistoricalWeatherResponse> GetHistoricalAsync(
        string location,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        if (to < from)
        {
            throw new ArgumentException("'to' must be after 'from'.", nameof(to));
        }

        var resolvedLocation = await ResolveLocationAsync(location, cancellationToken)
            ?? throw new KeyNotFoundException($"Location '{location}' not found.");

        var utcFrom = DateTime.SpecifyKind(from, DateTimeKind.Utc);
        var utcTo = DateTime.SpecifyKind(to, DateTimeKind.Utc);

        if (resolvedLocation.Type == "station")
        {
            var observations = await dbContext.WeatherObservations
                .Where(item => item.LocationId == resolvedLocation.Id
                    && item.ObservedAt >= utcFrom
                    && item.ObservedAt <= utcTo)
                .OrderBy(item => item.ObservedAt)
                .Select(item => new HistoricalObservationResponse(item.TemperatureCelsius, item.ObservedAt))
                .ToListAsync(cancellationToken);

            return new HistoricalWeatherResponse(
                resolvedLocation.Name,
                utcFrom,
                utcTo,
                observations.Count,
                observations,
                null);
        }

        var regionName = resolvedLocation.Region ?? resolvedLocation.Name;
        var regionLocation = await dbContext.Locations
            .FirstOrDefaultAsync(item => item.Name == regionName, cancellationToken) ?? resolvedLocation;

        var forecasts = await dbContext.ForecastEntries
            .Where(item => item.LocationId == regionLocation.Id
                && item.ValidFrom >= utcFrom
                && item.ValidFrom <= utcTo)
            .OrderBy(item => item.IssuedAt)
            .ThenBy(item => item.ValidFrom)
            .Select(item => new HistoricalForecastResponse(
                item.IssuedAt,
                item.ValidFrom,
                item.ValidTo,
                item.ForecastType,
                item.ForecastText))
            .ToListAsync(cancellationToken);

        return new HistoricalWeatherResponse(
            resolvedLocation.Name,
            utcFrom,
            utcTo,
            forecasts.Count,
            null,
            forecasts);
    }

    private Task<Location?> ResolveLocationAsync(string location, CancellationToken cancellationToken)
    {
        var loweredLocation = location.ToLower();

        return dbContext.Locations.FirstOrDefaultAsync(
            item => item.Name.ToLower() == loweredLocation || item.ExternalId == location,
            cancellationToken);
    }
}
