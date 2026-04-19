using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Weather.Domain.Entities;
using Weather.Infrastructure.Persistence;

namespace Weather.Seeder.Services;

public class SingaporeWeatherSeeder(
    WeatherDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    ILogger<SingaporeWeatherSeeder> logger)
{
    private static readonly string[] Regions = ["north", "south", "east", "west", "central"];
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRegionsAsync(cancellationToken);
        await SeedAirTemperatureAsync(cancellationToken);
        await SeedForecastAsync(cancellationToken);
    }

    private async Task EnsureRegionsAsync(CancellationToken cancellationToken)
    {
        var existing = (await dbContext.Locations
            .Where(location => location.Type == "region")
            .Select(location => location.Name)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var locations = Regions
            .Concat(["singapore"])
            .Where(region => !existing.Contains(region))
            .Select(region => new Location
            {
                Name = region,
                Type = "region",
                Region = region == "singapore" ? null : region,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            })
            .ToList();

        if (locations.Count == 0)
        {
            return;
        }

        dbContext.Locations.AddRange(locations);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Region locations ready.");
    }

    private async Task SeedAirTemperatureAsync(CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient("sg-weather");
            var response = await httpClient.GetFromJsonAsync<AirTemperatureResponse>(
                "air-temperature",
                JsonOptions,
                cancellationToken);

            if (response?.Data is null)
            {
                logger.LogWarning("Air temperature API returned no data.");
                return;
            }

            var existingStations = await dbContext.Locations
                .Where(location => location.Type == "station")
                .ToDictionaryAsync(location => location.ExternalId!, location => location.Id, cancellationToken);

            foreach (var station in response.Data.Stations)
            {
                if (existingStations.ContainsKey(station.Id))
                {
                    continue;
                }

                var location = new Location
                {
                    Name = station.Name,
                    Type = "station",
                    ExternalId = station.Id,
                    Latitude = station.Coordinates is not null ? (decimal)station.Coordinates.Latitude : null,
                    Longitude = station.Coordinates is not null ? (decimal)station.Coordinates.Longitude : null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                dbContext.Locations.Add(location);
                await dbContext.SaveChangesAsync(cancellationToken);
                existingStations[station.Id] = location.Id;
            }

            var inserted = 0;

            foreach (var readingGroup in response.Data.Readings)
            {
                var observedAt = readingGroup.Timestamp.UtcDateTime;

                foreach (var reading in readingGroup.Data)
                {
                    if (!existingStations.TryGetValue(reading.StationId, out var locationId))
                    {
                        continue;
                    }

                    var exists = await dbContext.WeatherObservations.AnyAsync(
                        observation => observation.LocationId == locationId && observation.ObservedAt == observedAt,
                        cancellationToken);

                    if (exists)
                    {
                        continue;
                    }

                    dbContext.WeatherObservations.Add(new WeatherObservation
                    {
                        LocationId = locationId,
                        ObservedAt = observedAt,
                        TemperatureCelsius = (decimal)reading.Value,
                        CreatedAt = DateTime.UtcNow
                    });

                    inserted++;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Air temperature seeded: {Count} new observations.", inserted);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to seed air temperature.");
        }
    }

    private async Task SeedForecastAsync(CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient("sg-weather");
            var response = await httpClient.GetFromJsonAsync<ForecastApiResponse>(
                "twenty-four-hr-forecast",
                JsonOptions,
                cancellationToken);

            if (response?.Data?.Records is null)
            {
                logger.LogWarning("Forecast API returned no data.");
                return;
            }

            var regionLocations = await dbContext.Locations
                .Where(location => Regions.Contains(location.Name))
                .ToDictionaryAsync(location => location.Name, location => location.Id, cancellationToken);

            var singaporeId = await dbContext.Locations
                .Where(location => location.Name == "singapore")
                .Select(location => location.Id)
                .FirstAsync(cancellationToken);

            var inserted = 0;

            foreach (var record in response.Data.Records)
            {
                var issuedAt = record.Timestamp.UtcDateTime;

                if (record.General is not null)
                {
                    var validFrom = record.General.ValidPeriod.Start.UtcDateTime;
                    var validTo = record.General.ValidPeriod.End.UtcDateTime;

                    var exists = await dbContext.ForecastEntries.AnyAsync(
                        entry => entry.LocationId == singaporeId
                            && entry.IssuedAt == issuedAt
                            && entry.ValidFrom == validFrom
                            && entry.ForecastType == "general",
                        cancellationToken);

                    if (!exists)
                    {
                        dbContext.ForecastEntries.Add(new ForecastEntry
                        {
                            LocationId = singaporeId,
                            IssuedAt = issuedAt,
                            ValidFrom = validFrom,
                            ValidTo = validTo,
                            ForecastType = "general",
                            ForecastText = record.General.Forecast.Text,
                            TempLowCelsius = (decimal)record.General.Temperature.Low,
                            TempHighCelsius = (decimal)record.General.Temperature.High,
                            HumidityLowPct = (decimal)record.General.RelativeHumidity.Low,
                            HumidityHighPct = (decimal)record.General.RelativeHumidity.High,
                            WindDirection = record.General.Wind.Direction,
                            WindSpeedLowKmh = (decimal)record.General.Wind.Speed.Low,
                            WindSpeedHighKmh = (decimal)record.General.Wind.Speed.High,
                            CreatedAt = DateTime.UtcNow
                        });

                        inserted++;
                    }
                }

                foreach (var period in record.Periods)
                {
                    var validFrom = period.TimePeriod.Start.UtcDateTime;
                    var validTo = period.TimePeriod.End.UtcDateTime;

                    foreach (var regionForecast in period.Regions.ToEnumerable())
                    {
                        var regionName = regionForecast.Region;
                        var forecast = regionForecast.Forecast;

                        if (!regionLocations.TryGetValue(regionName, out var locationId))
                        {
                            continue;
                        }

                        var exists = await dbContext.ForecastEntries.AnyAsync(
                            entry => entry.LocationId == locationId
                                && entry.IssuedAt == issuedAt
                                && entry.ValidFrom == validFrom
                                && entry.ForecastType == "period",
                            cancellationToken);

                        if (exists)
                        {
                            continue;
                        }

                        dbContext.ForecastEntries.Add(new ForecastEntry
                        {
                            LocationId = locationId,
                            IssuedAt = issuedAt,
                            ValidFrom = validFrom,
                            ValidTo = validTo,
                            ForecastType = "period",
                            ForecastText = forecast.Text,
                            CreatedAt = DateTime.UtcNow
                        });

                        inserted++;
                    }
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            logger.LogInformation("Forecast seeded: {Count} new entries.", inserted);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to seed forecast.");
        }
    }
}

internal sealed record AirTemperatureResponse([property: JsonPropertyName("data")] AirTemperatureData? Data);

internal sealed record AirTemperatureData(
    [property: JsonPropertyName("stations")] List<StationDto> Stations,
    [property: JsonPropertyName("readings")] List<ReadingGroupDto> Readings);

internal sealed record StationDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("location")] CoordinatesDto? Location,
    [property: JsonPropertyName("labelLocation")] CoordinatesDto? LabelLocation)
{
    public CoordinatesDto? Coordinates => Location ?? LabelLocation;
}

internal sealed record CoordinatesDto(
    [property: JsonPropertyName("latitude")] double Latitude,
    [property: JsonPropertyName("longitude")] double Longitude);

internal sealed record ReadingGroupDto(
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("data")] List<ReadingValueDto> Data);

internal sealed record ReadingValueDto(
    [property: JsonPropertyName("stationId")] string StationId,
    [property: JsonPropertyName("value")] double Value);

internal sealed record ForecastApiResponse([property: JsonPropertyName("data")] ForecastApiData? Data);

internal sealed record ForecastApiData([property: JsonPropertyName("records")] List<ForecastRecordDto>? Records);

internal sealed record ForecastRecordDto(
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("updatedTimestamp")] DateTimeOffset UpdatedTimestamp,
    [property: JsonPropertyName("general")] GeneralForecastDto? General,
    [property: JsonPropertyName("periods")] List<ForecastPeriodDto> Periods);

internal sealed record GeneralForecastDto(
    [property: JsonPropertyName("validPeriod")] TimePeriodDto ValidPeriod,
    [property: JsonPropertyName("temperature")] RangeDto Temperature,
    [property: JsonPropertyName("relativeHumidity")] RangeDto RelativeHumidity,
    [property: JsonPropertyName("forecast")] ForecastCodeDto Forecast,
    [property: JsonPropertyName("wind")] WindDto Wind);

internal sealed record TimePeriodDto(
    [property: JsonPropertyName("start")] DateTimeOffset Start,
    [property: JsonPropertyName("end")] DateTimeOffset End,
    [property: JsonPropertyName("text")] string Text);

internal sealed record RangeDto(
    [property: JsonPropertyName("low")] double Low,
    [property: JsonPropertyName("high")] double High);

internal sealed record ForecastCodeDto(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("text")] string Text);

internal sealed record WindDto(
    [property: JsonPropertyName("speed")] RangeDto Speed,
    [property: JsonPropertyName("direction")] string Direction);

internal sealed record ForecastPeriodDto(
    [property: JsonPropertyName("timePeriod")] TimePeriodDto TimePeriod,
    [property: JsonPropertyName("regions")] RegionsDto Regions);

internal sealed record RegionsDto(
    [property: JsonPropertyName("west")] ForecastCodeDto West,
    [property: JsonPropertyName("east")] ForecastCodeDto East,
    [property: JsonPropertyName("central")] ForecastCodeDto Central,
    [property: JsonPropertyName("north")] ForecastCodeDto North,
    [property: JsonPropertyName("south")] ForecastCodeDto South)
{
    public IEnumerable<(string Region, ForecastCodeDto Forecast)> ToEnumerable()
    {
        yield return ("west", West);
        yield return ("east", East);
        yield return ("central", Central);
        yield return ("north", North);
        yield return ("south", South);
    }
}
