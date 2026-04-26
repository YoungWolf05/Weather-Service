using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            KeyNotFoundException  => (StatusCodes.Status404NotFound,           "Not Found"),
            ArgumentException     => (StatusCodes.Status400BadRequest,          "Bad Request"),
            DbUpdateException dbu when IsUniqueConstraintViolation(dbu)
                                  => (StatusCodes.Status409Conflict,            "Conflict"),
            _                     => (StatusCodes.Status500InternalServerError, "Internal Server Error")
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
                Detail = statusCode == StatusCodes.Status409Conflict
                    ? "A subscription with the same email, location, and condition already exists."
                    : exception.Message
            },
            cancellationToken);

        return true;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        => ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("23505", StringComparison.OrdinalIgnoreCase) == true; // Postgres error code
}


