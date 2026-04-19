using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using WeatherApi.Data;
using WeatherApi.Data.Models;

namespace WeatherApi.Services;

public class WeatherSeeder(WeatherDbContext db, IHttpClientFactory httpFactory, ILogger<WeatherSeeder> logger)
{
    private static readonly string[] Regions = ["north", "south", "east", "west", "central"];
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task SeedAsync()
    {
        await EnsureRegionsAsync();
        await SeedAirTemperatureAsync();
        await SeedForecastAsync();
    }

    // ── 1. Seed the 6 static region/general location rows ──────────────────────
    private async Task EnsureRegionsAsync()
    {
        var existing = (await db.Locations
            .Where(l => l.Type == "region")
            .Select(l => l.Name)
            .ToListAsync()).ToHashSet();

        var toAdd = Regions
            .Concat(["singapore"])
            .Where(r => !existing.Contains(r))
            .Select(r => new Location
            {
                Name = r,
                Type = "region",
                Region = r == "singapore" ? null : r,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        db.Locations.AddRange(toAdd);
        await db.SaveChangesAsync();
        logger.LogInformation("Region locations ready.");
    }

    // ── 2. Air temperature → stations + observations ────────────────────────────
    private async Task SeedAirTemperatureAsync()
    {
        try
        {
            var http = httpFactory.CreateClient("sg-weather");
            var response = await http.GetFromJsonAsync<AirTempResponse>("air-temperature", JsonOpts);
            if (response?.Data is null) { logger.LogWarning("Air temperature API returned no data."); return; }

            // Upsert stations
            var existingStations = await db.Locations
                .Where(l => l.Type == "station")
                .ToDictionaryAsync(l => l.ExternalId!, l => l.Id);

            foreach (var s in response.Data.Stations)
            {
                if (!existingStations.ContainsKey(s.Id))
                {
                    var loc = new Location
                    {
                        Name = s.Name,
                        Type = "station",
                        ExternalId = s.Id,
                        Latitude  = s.Coords is not null ? (decimal)s.Coords.Latitude  : null,
                        Longitude = s.Coords is not null ? (decimal)s.Coords.Longitude : null,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    db.Locations.Add(loc);
                    await db.SaveChangesAsync();
                    existingStations[s.Id] = loc.Id;
                }
            }

            // Insert readings (skip duplicates via UNIQUE constraint)
            int inserted = 0;
            foreach (var reading in response.Data.Readings)
            {
                var ts = reading.Timestamp.UtcDateTime;
                foreach (var d in reading.Data)
                {
                    if (!existingStations.TryGetValue(d.StationId, out var locId)) continue;
                    bool exists = await db.WeatherObservations
                        .AnyAsync(o => o.LocationId == locId && o.ObservedAt == ts);
                    if (exists) continue;

                    db.WeatherObservations.Add(new WeatherObservation
                    {
                        LocationId = locId,
                        ObservedAt = ts,
                        TemperatureCelsius = (decimal)d.Value,
                        CreatedAt = DateTime.UtcNow
                    });
                    inserted++;
                }
            }
            await db.SaveChangesAsync();
            logger.LogInformation("Air temperature seeded: {Count} new observations.", inserted);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed air temperature.");
        }
    }

    // ── 3. 24h forecast → general + period entries ──────────────────────────────
    private async Task SeedForecastAsync()
    {
        try
        {
            var http = httpFactory.CreateClient("sg-weather");
            var response = await http.GetFromJsonAsync<ForecastResponse>("twenty-four-hr-forecast", JsonOpts);
            if (response?.Data?.Records is null) { logger.LogWarning("Forecast API returned no data."); return; }

            var regionLocs = await db.Locations
                .Where(l => Regions.Contains(l.Name))
                .ToDictionaryAsync(l => l.Name, l => l.Id);

            var singaporeId = await db.Locations
                .Where(l => l.Name == "singapore")
                .Select(l => l.Id)
                .FirstAsync();

            int inserted = 0;
            foreach (var record in response.Data.Records)
            {
                var issuedAt = record.Timestamp.UtcDateTime;

                // General forecast
                if (record.General is not null)
                {
                    var vFrom = record.General.ValidPeriod.Start.UtcDateTime;
                    var vTo   = record.General.ValidPeriod.End.UtcDateTime;

                    bool exists = await db.ForecastEntries.AnyAsync(f =>
                        f.LocationId == singaporeId &&
                        f.IssuedAt   == issuedAt    &&
                        f.ValidFrom  == vFrom       &&
                        f.ForecastType == "general");

                    if (!exists)
                    {
                        db.ForecastEntries.Add(new ForecastEntry
                        {
                            LocationId       = singaporeId,
                            IssuedAt         = issuedAt,
                            ValidFrom        = vFrom,
                            ValidTo          = vTo,
                            ForecastType     = "general",
                            ForecastText     = record.General.Forecast.Text,
                            TempLowCelsius   = (decimal)record.General.Temperature.Low,
                            TempHighCelsius  = (decimal)record.General.Temperature.High,
                            HumidityLowPct   = (decimal)record.General.RelativeHumidity.Low,
                            HumidityHighPct  = (decimal)record.General.RelativeHumidity.High,
                            WindDirection    = record.General.Wind.Direction,
                            WindSpeedLowKmh  = (decimal)record.General.Wind.Speed.Low,
                            WindSpeedHighKmh = (decimal)record.General.Wind.Speed.High,
                            CreatedAt        = DateTime.UtcNow
                        });
                        inserted++;
                    }
                }

                // Period forecasts per region
                foreach (var period in record.Periods)
                {
                    var tFrom = period.TimePeriod.Start.UtcDateTime;
                    var tTo   = period.TimePeriod.End.UtcDateTime;

                    foreach (var (regionName, forecast) in period.Regions.ToEnumerable())
                    {
                        if (!regionLocs.TryGetValue(regionName, out var locId)) continue;

                        bool exists = await db.ForecastEntries.AnyAsync(f =>
                            f.LocationId == locId     &&
                            f.IssuedAt   == issuedAt  &&
                            f.ValidFrom  == tFrom     &&
                            f.ForecastType == "period");

                        if (!exists)
                        {
                            db.ForecastEntries.Add(new ForecastEntry
                            {
                                LocationId   = locId,
                                IssuedAt     = issuedAt,
                                ValidFrom    = tFrom,
                                ValidTo      = tTo,
                                ForecastType = "period",
                                ForecastText = forecast.Text,
                                CreatedAt    = DateTime.UtcNow
                            });
                            inserted++;
                        }
                    }
                }

                await db.SaveChangesAsync();
            }
            logger.LogInformation("Forecast seeded: {Count} new entries.", inserted);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed forecast.");
        }
    }
}

// ── Singapore API response DTOs ──────────────────────────────────────────────

// Air Temperature
record AirTempResponse([property: JsonPropertyName("data")] AirTempData? Data);
record AirTempData(
    [property: JsonPropertyName("stations")] List<StationDto> Stations,
    [property: JsonPropertyName("readings")] List<ReadingGroupDto> Readings);
record StationDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("location")] CoordDto? Location,           // actual API field
    [property: JsonPropertyName("labelLocation")] CoordDto? LabelLocation) // spec field (fallback)
{
    public CoordDto? Coords => Location ?? LabelLocation;
}
record CoordDto(
    [property: JsonPropertyName("latitude")] double Latitude,
    [property: JsonPropertyName("longitude")] double Longitude);
record ReadingGroupDto(
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("data")] List<ReadingValueDto> Data);
record ReadingValueDto(
    [property: JsonPropertyName("stationId")] string StationId,
    [property: JsonPropertyName("value")] double Value);

// 24-Hour Forecast
record ForecastResponse([property: JsonPropertyName("data")] ForecastData? Data);
record ForecastData([property: JsonPropertyName("records")] List<ForecastRecord>? Records);
record ForecastRecord(
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("updatedTimestamp")] DateTimeOffset UpdatedTimestamp,
    [property: JsonPropertyName("general")] GeneralForecast? General,
    [property: JsonPropertyName("periods")] List<ForecastPeriod> Periods);
record GeneralForecast(
    [property: JsonPropertyName("validPeriod")] TimePeriodDto ValidPeriod,
    [property: JsonPropertyName("temperature")] RangeDto Temperature,
    [property: JsonPropertyName("relativeHumidity")] RangeDto RelativeHumidity,
    [property: JsonPropertyName("forecast")] ForecastCodeDto Forecast,
    [property: JsonPropertyName("wind")] WindDto Wind);
record TimePeriodDto(
    [property: JsonPropertyName("start")] DateTimeOffset Start,
    [property: JsonPropertyName("end")] DateTimeOffset End,
    [property: JsonPropertyName("text")] string Text);
record RangeDto(
    [property: JsonPropertyName("low")] double Low,
    [property: JsonPropertyName("high")] double High);
record ForecastCodeDto(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("text")] string Text);
record WindDto(
    [property: JsonPropertyName("speed")] RangeDto Speed,
    [property: JsonPropertyName("direction")] string Direction);
record ForecastPeriod(
    [property: JsonPropertyName("timePeriod")] TimePeriodDto TimePeriod,
    [property: JsonPropertyName("regions")] RegionsDto Regions);
record RegionsDto(
    [property: JsonPropertyName("west")] ForecastCodeDto West,
    [property: JsonPropertyName("east")] ForecastCodeDto East,
    [property: JsonPropertyName("central")] ForecastCodeDto Central,
    [property: JsonPropertyName("north")] ForecastCodeDto North,
    [property: JsonPropertyName("south")] ForecastCodeDto South)
{
    public IEnumerable<(string Region, ForecastCodeDto Forecast)> ToEnumerable()
    {
        yield return ("west",    West);
        yield return ("east",    East);
        yield return ("central", Central);
        yield return ("north",   North);
        yield return ("south",   South);
    }
}
