using Weather.Application.Abstractions;

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

    static async Task<IResult> GetCurrentAsync(
        string location,
        IWeatherQueryService weatherQueryService,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await weatherQueryService.GetCurrentAsync(location, cancellationToken);
            return Results.Ok(response);
        }
        catch (KeyNotFoundException exception)
        {
            return Results.NotFound(new { error = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new { error = exception.Message });
        }
    }

    static async Task<IResult> GetForecastAsync(
        string location,
        IWeatherQueryService weatherQueryService,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await weatherQueryService.GetForecastAsync(location, cancellationToken);
            return Results.Ok(response);
        }
        catch (KeyNotFoundException exception)
        {
            return Results.NotFound(new { error = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new { error = exception.Message });
        }
    }

    static async Task<IResult> GetHistoricalAsync(
        string location,
        DateTime from,
        DateTime to,
        IWeatherQueryService weatherQueryService,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await weatherQueryService.GetHistoricalAsync(location, from, to, cancellationToken);
            return Results.Ok(response);
        }
        catch (KeyNotFoundException exception)
        {
            return Results.NotFound(new { error = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new { error = exception.Message });
        }
    }

    static async Task<IResult> GetLocationsAsync(
        string? type,
        IWeatherQueryService weatherQueryService,
        CancellationToken cancellationToken)
    {
        var response = await weatherQueryService.GetLocationsAsync(type, cancellationToken);
        return Results.Ok(response);
    }
}
