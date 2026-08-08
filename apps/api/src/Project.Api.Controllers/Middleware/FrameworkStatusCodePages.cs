using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Project.Api.Controllers.Middleware;

/// <summary>
/// Handles framework-generated status codes (404 from routing, 405 from method mismatch)
/// by emitting RFC 7807 ProblemDetails with stable public codes.
/// </summary>
internal static class FrameworkStatusCodePages
{
    private const string GenericNotFoundDetail = "The requested resource was not found.";
    private const string GenericMethodNotAllowedDetail = "The HTTP method is not allowed for this resource.";

    /// <summary>
    /// Stable public code for framework-generated 404 responses.
    /// </summary>
    public const string RoutingNotFound = "ROUTING_NOT_FOUND";

    /// <summary>
    /// Stable public code for framework-generated 405 responses.
    /// </summary>
    public const string MethodNotAllowed = "METHOD_NOT_ALLOWED";

    /// <summary>
    /// Writes a ProblemDetails response for 404/405 status codes.
    /// Guards against double-write when the response is already owned by downstream middleware.
    /// </summary>
    public static async Task WriteAsync(StatusCodeContext context)
    {
        var statusCode = context.HttpContext.Response.StatusCode;

        // Only handle 404 and 405
        if (statusCode != StatusCodes.Status404NotFound &&
            statusCode != StatusCodes.Status405MethodNotAllowed)
        {
            return;
        }

        var response = context.HttpContext.Response;

        // Guard: do not double-write if downstream middleware already owns the response
        if (response.HasStarted ||
            response.ContentLength.HasValue ||
            !string.IsNullOrEmpty(response.ContentType))
        {
            return;
        }

        // Map status code to stable public code and generic detail
        var (code, detail) = statusCode switch
        {
            StatusCodes.Status404NotFound => (RoutingNotFound, GenericNotFoundDetail),
            StatusCodes.Status405MethodNotAllowed => (MethodNotAllowed, GenericMethodNotAllowedDetail),
            _ => (null, null)
        };

        if (code is null || detail is null)
        {
            return;
        }

        // Build ProblemDetails using the existing factory
        var problemDetails = ProblemDetailsResponseFactory.Create(statusCode, code, detail);

        // Write via IProblemDetailsService for consistent serialization
        var problemDetailsService = context.HttpContext.RequestServices
            .GetRequiredService<IProblemDetailsService>();

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails = problemDetails
        });
    }
}
