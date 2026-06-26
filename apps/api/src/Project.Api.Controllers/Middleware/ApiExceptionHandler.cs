using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Project.Api.Controllers.Middleware;

/// <summary>
/// Global exception handler that catches all unhandled exceptions and returns
/// safe, generic 500 <see cref="ProblemDetails"/> responses.
///
/// <list type="bullet">
/// <item>Never exposes exception messages or stack traces in the response body.</item>
/// <item>Treats <see cref="OperationCanceledException"/> and <see cref="TaskCanceledException"/>
///       as expected request-abort paths — logs at Information level without 500 telemetry noise.</item>
/// <item>All other exceptions are logged at Error level before returning the safe response.</item>
/// </list>
/// </summary>
internal sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Request-aborted cancellations are expected — log quietly and suppress the 500.
        if (exception is OperationCanceledException or TaskCanceledException)
        {
            _logger.LogInformation(
                exception,
                "Request was cancelled — no ProblemDetails emitted for client disconnect");
            return false; // Let the default handler deal with it (it won't write a body)
        }

        // Real exception — log at Error level for server-side diagnostics.
        _logger.LogError(exception, "Unhandled exception caught by global handler");

        // Build a safe, generic 500 ProblemDetails — NEVER expose exception details.
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = "An unexpected error occurred. Please try again later.",
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        // Return true: handler took ownership — default handler won't run.
        // IMPORTANT: This suppresses the exception diagnostics middleware for this
        // exception, so ensure it is logged before returning true.
        return true;
    }
}
