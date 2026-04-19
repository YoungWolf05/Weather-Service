using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Weather.Api.Exceptions;

/// <summary>
/// Centrally maps domain exceptions to RFC-7807 ProblemDetails responses,
/// eliminating try/catch blocks from every controller action.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            ArgumentException    => (StatusCodes.Status400BadRequest, "Bad Request"),
            _                    => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        logger.LogError(
            exception,
            "HTTP {StatusCode} — {Title}: {Message}",
            statusCode, title, exception.Message);

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = statusCode,
                Title  = title,
                Detail = exception.Message
            },
            cancellationToken);

        return true;
    }
}
