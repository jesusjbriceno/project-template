using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
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
    private readonly IProblemDetailsService _problemDetailsService;

    public ApiExceptionHandler(
        ILogger<ApiExceptionHandler> logger,
        IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
        {
            _logger.LogWarning(exception, "Response already started; cannot write ProblemDetails safely");
            return false; // Let the default handler deal with it (it won't write a body)
        }

        var isCancellationException = exception is OperationCanceledException or TaskCanceledException;
        var isClientCancellation = isCancellationException && httpContext.RequestAborted.IsCancellationRequested;

        if (isClientCancellation)
        {
            _logger.LogInformation(
                exception,
                "Request was cancelled by the client; suppressing ProblemDetails response");
            return true;
        }

        // Real exception — log at Error level for server-side diagnostics.
        _logger.LogError(exception, "Unhandled exception caught by global handler");

        var problemDetails = ProblemDetailsResponseFactory.Create(
            StatusCodes.Status500InternalServerError,
            "UNHANDLED_EXCEPTION",
            null);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await _problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        });

        // Return true: handler took ownership — default handler won't run.
        // IMPORTANT: This suppresses the exception diagnostics middleware for this
        // exception, so ensure it is logged before returning true.
        return true;
    }
}
