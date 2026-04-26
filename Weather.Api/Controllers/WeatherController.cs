using CsvHelper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using Weather.Domain.Contracts.Weather;
using Weather.Application.Weather.Queries.ExportCurrentWeather;
using Weather.Application.Weather.Queries.GetCurrentWeather;
using Weather.Application.Weather.Queries.GetForecast;
using Weather.Application.Weather.Queries.GetHistoricalWeather;
using Weather.Application.Weather.Queries.GetLocations;
using Weather.Api.Csv;

namespace Weather.Api.Controllers;

/// <summary>
/// Query endpoints for Singapore weather data — current conditions,
/// 24-hour forecasts, historical records, and location listings.
/// </summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class WeatherController(ISender sender) : ControllerBase
{
    /// <summary>List all queryable locations.</summary>
    /// <param name="type">Optional filter: <c>station</c> or <c>region</c>.</param>
    /// <param name="cancellationToken"></param>
    [HttpGet("locations")]
    [ProducesResponseType<IReadOnlyList<LocationResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLocationsAsync(
        [FromQuery] string? type,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetLocationsQuery(type), cancellationToken);
        return Ok(result);
    }

    /// <summary>Get current weather for a location.</summary>
    /// <remarks>
    /// Pass a station name or external ID for the latest temperature reading.
    /// Pass a region name (<c>north</c>, <c>south</c>, <c>east</c>, <c>west</c>, <c>central</c>)
    /// for the active period forecast.
    /// </remarks>
    /// <param name="location">Station name/ID or region name.</param>
    /// <param name="cancellationToken"></param>
    [HttpGet("weather/current")]
    [ProducesResponseType<CurrentWeatherResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentAsync(
        [FromQuery] string location,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCurrentWeatherQuery(location), cancellationToken);
        return Ok(result);
    }

    /// <summary>Get the latest 24-hour forecast for a region.</summary>
    /// <remarks>
    /// Accepts a region name: <c>north</c>, <c>south</c>, <c>east</c>, <c>west</c>, or <c>central</c>.
    /// Returns the general outlook plus individual period breakdowns.
    /// </remarks>
    /// <param name="location">Region name.</param>
    /// <param name="cancellationToken"></param>
    [HttpGet("weather/forecast")]
    [ProducesResponseType<ForecastResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForecastAsync(
        [FromQuery] string location,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetForecastQuery(location), cancellationToken);
        return Ok(result);
    }

    /// <summary>Get historical weather for a date range.</summary>
    /// <remarks>
    /// Returns temperature observations for stations or past forecast entries for regions.
    /// Both <paramref name="from"/> and <paramref name="to"/> are UTC.
    /// </remarks>
    /// <param name="location">Station name/ID or region name.</param>
    /// <param name="from">Range start (UTC).</param>
    /// <param name="to">Range end (UTC).</param>
    /// <param name="cancellationToken"></param>
    [HttpGet("weather/historical")]
    [ProducesResponseType<HistoricalWeatherResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistoricalAsync(
        [FromQuery] string location,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetHistoricalWeatherQuery(location, from, to), cancellationToken);
        return Ok(result);
    }

    /// <summary>Export current weather for a location as a CSV file.</summary>
    /// <remarks>
    /// Returns a single-row CSV with all available observation and forecast fields flattened.
    /// Null fields are exported as empty cells.
    /// </remarks>
    /// <param name="location">Station name/ID or region name.</param>
    /// <param name="cancellationToken"></param>
    [HttpGet("weather/current/export")]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportCurrentAsync(
        [FromQuery] string location,
        CancellationToken cancellationToken)
    {
        var row = await sender.Send(new ExportCurrentWeatherQuery(location), cancellationToken);

        var stream = new MemoryStream();
        await using (var writer = new StreamWriter(stream, leaveOpen: true))
        await using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.Context.RegisterClassMap<WeatherExportMap>();
            csv.WriteHeader<CurrentWeatherExportDto>();
            await csv.NextRecordAsync();
            csv.WriteRecord(row);
            await csv.NextRecordAsync();
        }

        stream.Position = 0;
        return File(stream, "text/csv", $"weather-{location}.csv");
    }
}


