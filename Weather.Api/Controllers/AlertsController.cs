using MediatR;
using Microsoft.AspNetCore.Mvc;
using Weather.Application.Alerts.Commands.EvaluateAlerts;
using Weather.Application.Alerts.Commands.Subscribe;
using Weather.Application.Alerts.Commands.Unsubscribe;
using Weather.Application.Alerts.Queries.GetSubscriptions;
using Weather.Application.Alerts.Queries.GetTriggeredAlerts;
using Weather.Application.Contracts;

namespace Weather.Api.Controllers;

/// <summary>
/// Manage weather alert subscriptions and poll triggered alerts.
/// Alerts fire automatically after each data seed run.
/// </summary>
[ApiController]
[Route("api/alerts")]
[Produces("application/json")]
public class AlertsController(ISender sender) : ControllerBase
{
    /// <summary>Subscribe to a temperature threshold alert for a weather station.</summary>
    /// <remarks>
    /// Only station locations are supported (not regions).
    /// <paramref name="condition"/> must be <c>"above"</c> or <c>"below"</c>.
    /// An exact duplicate subscription (same email + station + condition) is rejected with 400.
    /// </remarks>
    [HttpPost("subscriptions")]
    [ProducesResponseType<AlertSubscriptionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubscribeAsync(
        [FromQuery] string email,
        [FromQuery] string location,
        [FromQuery] decimal thresholdCelsius,
        [FromQuery] string condition,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SubscribeToAlertCommand(email, location, thresholdCelsius, condition),
            cancellationToken);

        return Created($"/api/alerts/subscriptions?email={Uri.EscapeDataString(result.Email)}", result);
    }

    /// <summary>Unsubscribe (deactivate) an alert subscription.</summary>
    /// <remarks>
    /// The <paramref name="email"/> query parameter must match the subscription owner.
    /// The subscription record is kept but marked inactive — it will no longer fire.
    /// </remarks>
    /// <param name="id">Subscription ID to deactivate.</param>
    /// <param name="email">Owner email for ownership verification.</param>
    /// <param name="cancellationToken"></param>
    [HttpDelete("subscriptions/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnsubscribeAsync(
        int id,
        [FromQuery] string email,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UnsubscribeFromAlertCommand(id, email), cancellationToken);
        return NoContent();
    }

    /// <summary>List all subscriptions for a given email address.</summary>
    /// <param name="email">Subscriber's email address.</param>
    /// <param name="cancellationToken"></param>
    [HttpGet("subscriptions")]
    [ProducesResponseType<IReadOnlyList<AlertSubscriptionResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptionsAsync(
        [FromQuery] string email,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSubscriptionsQuery(email), cancellationToken);
        return Ok(result);
    }

    /// <summary>Poll triggered alerts for a subscriber.</summary>
    /// <remarks>
    /// Returns the most recent alerts, newest first.
    /// Alerts are created automatically after each data seed run whenever a subscribed station
    /// reading crosses the configured threshold.
    /// </remarks>
    /// <param name="email">Subscriber's email address.</param>
    /// <param name="limit">Maximum number of results to return (default 50).</param>
    /// <param name="cancellationToken"></param>
    [HttpGet("triggered")]
    [ProducesResponseType<IReadOnlyList<TriggeredAlertResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTriggeredAsync(
        [FromQuery] string email,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetTriggeredAlertsQuery(email, limit), cancellationToken);
        return Ok(result);
    }

    /// <summary>Manually trigger alert evaluation against the latest observations.</summary>
    /// <remarks>
    /// Useful for testing and ad-hoc checks. Evaluation also runs automatically after each data seed.
    /// Returns the number of new alerts fired in this evaluation pass.
    /// </remarks>
    [HttpPost("evaluate")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public async Task<IActionResult> EvaluateAsync(CancellationToken cancellationToken)
    {
        var fired = await sender.Send(new EvaluateAlertsCommand(), cancellationToken);
        return Ok(new { alertsFired = fired });
    }
}
