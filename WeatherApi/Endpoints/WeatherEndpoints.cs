using Microsoft.EntityFrameworkCore;
using WeatherApi.Data;
using WeatherApi.Data.Models;

namespace WeatherApi.Endpoints;

public static class WeatherEndpoints
{
    public static void MapWeatherEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/weather").WithTags("Weather");

        g.MapGet("/current",    GetCurrentAsync)
         .WithSummary("Get current weather by location (station name/ID for temperature, region for forecast)");

        g.MapGet("/forecast",   GetForecastAsync)
         .WithSummary("Get latest 24 h forecast breakdown by region (north/south/east/west/central)");

        g.MapGet("/historical", GetHistoricalAsync)
         .WithSummary("Get historical observations (station) or past forecasts (region) for a date range");

        app.MapGet("/api/locations", GetLocationsAsync)
           .WithTags("Weather")
           .WithSummary("List all queryable locations (stations and regions)");
    }

    // GET /api/weather/current?location={name|externalId}
    static async Task<IResult> GetCurrentAsync(string location, WeatherDbContext db, CancellationToken ct)
    {
        var loc = await ResolveLocation(location, db, ct);
        if (loc is null)
            return Results.NotFound(new { error = $"Location '{location}' not found." });

        WeatherObservation? obs = null;
        ForecastEntry? general = null;
        ForecastEntry? period  = null;

        if (loc.Type == "station")
        {
            obs = await db.WeatherObservations
                .Where(o => o.LocationId == loc.Id)
                .OrderByDescending(o => o.ObservedAt)
                .FirstOrDefaultAsync(ct);
        }

        // Derive region: region locations use their own Name, stations use their Region field
        var regionName = loc.Type == "region"
            ? (loc.Region ?? "singapore")
            : loc.Region;

        if (regionName is not null)
        {
            var now = DateTime.UtcNow;

            var latestIssued = await db.ForecastEntries
                .Where(f => f.ForecastType == "period" && f.ValidFrom <= now && f.ValidTo >= now)
                .MaxAsync(f => (DateTime?)f.IssuedAt, ct);

            if (latestIssued.HasValue)
            {
                var regionLoc = await db.Locations.FirstOrDefaultAsync(l => l.Name == regionName, ct);
                var sgLoc     = await db.Locations.FirstOrDefaultAsync(l => l.Name == "singapore", ct);

                if (regionLoc is not null)
                    period = await db.ForecastEntries
                        .Where(f => f.LocationId   == regionLoc.Id
                               && f.ForecastType   == "period"
                               && f.IssuedAt       == latestIssued
                               && f.ValidFrom      <= now
                               && f.ValidTo        >= now)
                        .FirstOrDefaultAsync(ct);

                if (sgLoc is not null)
                    general = await db.ForecastEntries
                        .Where(f => f.LocationId == sgLoc.Id
                               && f.ForecastType  == "general"
                               && f.IssuedAt      == latestIssued)
                        .FirstOrDefaultAsync(ct);
            }
        }

        return Results.Ok(new
        {
            location   = loc.Name,
            type       = loc.Type,
            externalId = loc.ExternalId,
            region     = loc.Region,
            observation = obs is null ? null : new
            {
                temperatureCelsius = obs.TemperatureCelsius,
                observedAt         = obs.ObservedAt
            },
            forecast = (period is null && general is null) ? null : new
            {
                condition        = period?.ForecastText ?? general?.ForecastText,
                tempLowCelsius   = general?.TempLowCelsius,
                tempHighCelsius  = general?.TempHighCelsius,
                humidityLowPct   = general?.HumidityLowPct,
                humidityHighPct  = general?.HumidityHighPct,
                windDirection    = general?.WindDirection,
                windSpeedLowKmh  = general?.WindSpeedLowKmh,
                windSpeedHighKmh = general?.WindSpeedHighKmh,
                validFrom        = period?.ValidFrom ?? general?.ValidFrom,
                validTo          = period?.ValidTo   ?? general?.ValidTo,
                issuedAt         = period?.IssuedAt  ?? general?.IssuedAt
            }
        });
    }

    // GET /api/weather/forecast?location={region}
    static async Task<IResult> GetForecastAsync(string location, WeatherDbContext db, CancellationToken ct)
    {
        var loc = await ResolveLocation(location, db, ct);
        if (loc is null)
            return Results.NotFound(new { error = $"Location '{location}' not found." });

        var regionName = loc.Type == "region" ? (loc.Region ?? "singapore") : loc.Region;
        if (regionName is null)
            return Results.BadRequest(new { error = "Provide a region name (north/south/east/west/central)." });

        var regionLoc = await db.Locations.FirstOrDefaultAsync(l => l.Name == regionName, ct);
        var sgLoc     = await db.Locations.FirstOrDefaultAsync(l => l.Name == "singapore", ct);
        if (regionLoc is null)
            return Results.NotFound(new { error = $"Region '{regionName}' not found." });

        // Latest issued batch that covers this region
        var latestIssued = await db.ForecastEntries
            .Where(f => f.LocationId == regionLoc.Id)
            .MaxAsync(f => (DateTime?)f.IssuedAt, ct);

        if (latestIssued is null)
            return Results.NotFound(new { error = "No forecast data available yet. Try again after seeding." });

        ForecastEntry? general = null;
        if (sgLoc is not null)
            general = await db.ForecastEntries
                .Where(f => f.LocationId  == sgLoc.Id
                       && f.ForecastType   == "general"
                       && f.IssuedAt       == latestIssued)
                .FirstOrDefaultAsync(ct);

        var periods = await db.ForecastEntries
            .Where(f => f.LocationId  == regionLoc.Id
                   && f.ForecastType   == "period"
                   && f.IssuedAt       == latestIssued)
            .OrderBy(f => f.ValidFrom)
            .ToListAsync(ct);

        return Results.Ok(new
        {
            region   = regionName,
            issuedAt = latestIssued,
            general  = general is null ? null : new
            {
                validFrom        = general.ValidFrom,
                validTo          = general.ValidTo,
                condition        = general.ForecastText,
                tempLowCelsius   = general.TempLowCelsius,
                tempHighCelsius  = general.TempHighCelsius,
                humidityLowPct   = general.HumidityLowPct,
                humidityHighPct  = general.HumidityHighPct,
                windDirection    = general.WindDirection,
                windSpeedLowKmh  = general.WindSpeedLowKmh,
                windSpeedHighKmh = general.WindSpeedHighKmh
            },
            periods = periods.Select(p => new
            {
                validFrom = p.ValidFrom,
                validTo   = p.ValidTo,
                condition = p.ForecastText
            })
        });
    }

    // GET /api/weather/historical?location={name}&from={date}&to={date}
    static async Task<IResult> GetHistoricalAsync(
        string location, DateTime from, DateTime to, WeatherDbContext db, CancellationToken ct)
    {
        if (to < from)
            return Results.BadRequest(new { error = "'to' must be after 'from'." });

        var loc = await ResolveLocation(location, db, ct);
        if (loc is null)
            return Results.NotFound(new { error = $"Location '{location}' not found." });

        var utcFrom = DateTime.SpecifyKind(from, DateTimeKind.Utc);
        var utcTo   = DateTime.SpecifyKind(to,   DateTimeKind.Utc);

        if (loc.Type == "station")
        {
            var obs = await db.WeatherObservations
                .Where(o => o.LocationId == loc.Id
                       && o.ObservedAt   >= utcFrom
                       && o.ObservedAt   <= utcTo)
                .OrderBy(o => o.ObservedAt)
                .ToListAsync(ct);

            return Results.Ok(new
            {
                location     = loc.Name,
                from         = utcFrom,
                to           = utcTo,
                count        = obs.Count,
                observations = obs.Select(o => new
                {
                    temperatureCelsius = o.TemperatureCelsius,
                    observedAt         = o.ObservedAt
                })
            });
        }
        else
        {
            // Region — return historical forecast entries
            var regionName = loc.Region ?? loc.Name;
            var regionLoc  = await db.Locations.FirstOrDefaultAsync(l => l.Name == regionName, ct) ?? loc;

            var forecasts = await db.ForecastEntries
                .Where(f => f.LocationId == regionLoc.Id
                       && f.ValidFrom    >= utcFrom
                       && f.ValidFrom    <= utcTo)
                .OrderBy(f => f.IssuedAt).ThenBy(f => f.ValidFrom)
                .ToListAsync(ct);

            return Results.Ok(new
            {
                location  = loc.Name,
                from      = utcFrom,
                to        = utcTo,
                count     = forecasts.Count,
                forecasts = forecasts.Select(f => new
                {
                    issuedAt     = f.IssuedAt,
                    validFrom    = f.ValidFrom,
                    validTo      = f.ValidTo,
                    forecastType = f.ForecastType,
                    condition    = f.ForecastText
                })
            });
        }
    }

    // GET /api/locations
    static async Task<IResult> GetLocationsAsync(string? type, WeatherDbContext db, CancellationToken ct)
    {
        var q = db.Locations.AsQueryable();
        if (type is not null) q = q.Where(l => l.Type == type);

        var locs = await q
            .OrderBy(l => l.Type).ThenBy(l => l.Name)
            .Select(l => new
            {
                l.Id, l.Name, l.Type, l.ExternalId, l.Region, l.Latitude, l.Longitude
            })
            .ToListAsync(ct);

        return Results.Ok(locs);
    }

    // ── helpers ────────────────────────────────────────────────────────────────
    private static Task<Location?> ResolveLocation(string location, WeatherDbContext db, CancellationToken ct) =>
        db.Locations.FirstOrDefaultAsync(
            l => l.Name.ToLower() == location.ToLower() || l.ExternalId == location, ct);
}
