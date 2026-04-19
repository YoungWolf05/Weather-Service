using MediatR;
using Weather.Application.Weather.Queries.GetCurrentWeather;
using Weather.Application.Weather.Queries.GetForecast;
using Weather.Application.Weather.Queries.GetHistoricalWeather;
using Weather.Application.Weather.Queries.GetLocations;

namespace Weather.Api.Endpoints;

public static class WeatherEndpoints
{
    public static void MapWeatherEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/weather").WithTags("Weather");

        g.MapGet("/current", GetCurrentAsync)
         .WithSummary("Get current weather by location (station name/ID for temperature, region for forecast)");

        g.MapGet("/forecast", GetForecastAsync)
         .WithSummary("Get latest 24 h forecast breakdown by region (north/south/east/west/central)");

        g.MapGet("/historical", GetHistoricalAsync)
         .WithSummary("Get historical observations (station) or past forecasts (region) for a date range");

        app.MapGet("/api/locations", GetLocationsAsync)
           .WithTags("Weather")
           .WithSummary("List all queryable locations (stations and regions)");
    }

    static async Task<IResult> GetCurrentAsync(
        string location,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await sender.Send(new GetCurrentWeatherQuery(location), cancellationToken);
            return Results.Ok(response);
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
        catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
    }

    static async Task<IResult> GetForecastAsync(
        string location,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await sender.Send(new GetForecastQuery(location), cancellationToken);
            return Results.Ok(response);
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
        catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
    }

    static async Task<IResult> GetHistoricalAsync(
        string location,
        DateTime from,
        DateTime to,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await sender.Send(new GetHistoricalWeatherQuery(location, from, to), cancellationToken);
            return Results.Ok(response);
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
        catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
    }

    static async Task<IResult> GetLocationsAsync(
        string? type,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetLocationsQuery(type), cancellationToken);
        return Results.Ok(response);
    }
}
