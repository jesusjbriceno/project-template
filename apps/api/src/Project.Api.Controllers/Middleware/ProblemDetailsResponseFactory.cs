using Microsoft.AspNetCore.Mvc;

namespace Project.Api.Controllers.Middleware;

internal static class ProblemDetailsResponseFactory
{
    private const string SafeServerErrorDetail = "An unexpected error occurred. Please try again later.";

    public static ProblemDetails Create(int statusCode, string code, string? detail)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = GetReasonPhrase(statusCode),
            Type = GetTypeUri(statusCode),
            Detail = statusCode == StatusCodes.Status500InternalServerError
                ? SafeServerErrorDetail
                : detail
        };

        problemDetails.Extensions["code"] = code;

        return problemDetails;
    }

    public static string GetTypeUri(int statusCode) => $"https://httpstatuses.com/{statusCode}";

    public static string GetSafeServerErrorDetail() => SafeServerErrorDetail;

    private static string GetReasonPhrase(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status405MethodNotAllowed => "Method Not Allowed",
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status422UnprocessableEntity => "Unprocessable Entity",
        StatusCodes.Status429TooManyRequests => "Too Many Requests",
        StatusCodes.Status500InternalServerError => "Internal Server Error",
        _ => $"HTTP {statusCode}"
    };
}
