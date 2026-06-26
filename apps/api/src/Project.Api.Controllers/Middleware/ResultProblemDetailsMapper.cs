using Microsoft.AspNetCore.Mvc;
using Project.Application.Common;

namespace Project.Api.Controllers.Middleware;

/// <summary>
/// Maps Application <see cref="Result"/> and <see cref="Result{T}"/> failures to
/// RFC 7807 <see cref="ProblemDetails"/> responses.
///
/// The caller (controller) is responsible for providing a safe, generic detail
/// message for auth errors to prevent user enumeration. The mapper passes the
/// detail through unchanged.
///
/// Returns <see cref="IActionResult"/> to keep the contract honest for callers
/// that may need to compose or further transform the result.
/// </summary>
internal static class ResultProblemDetailsMapper
{
    /// <summary>
    /// Maps a failed <see cref="Result"/> to an <see cref="IActionResult"/> with
    /// a <see cref="ProblemDetails"/> body, preserving the error code in
    /// <c>extensions.code</c>.
    /// </summary>
    public static IActionResult Map(Result result)
    {
        var statusCode = ErrorCodeToHttpStatus.GetStatusCode(result.Error.Code);
        var problemDetails = BuildProblemDetails(statusCode, result.Error.Code, result.Error.Message);

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }

    /// <summary>
    /// Maps a failed <see cref="Result{T}"/> to an <see cref="IActionResult"/> with
    /// a <see cref="ProblemDetails"/> body, preserving the error code in
    /// <c>extensions.code</c>.
    /// </summary>
    public static IActionResult Map<T>(Result<T> result)
    {
        var statusCode = ErrorCodeToHttpStatus.GetStatusCode(result.Error.Code);
        var problemDetails = BuildProblemDetails(statusCode, result.Error.Code, result.Error.Message);

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }

    private static ProblemDetails BuildProblemDetails(int statusCode, string code, string detail)
    {
        var title = GetReasonPhrase(statusCode);

        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Extensions = { ["code"] = code }
        };
    }

    /// <summary>
    /// Returns a human-readable reason phrase for common HTTP status codes.
    /// Falls back to the status code number for uncommon codes.
    /// </summary>
    private static string GetReasonPhrase(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        405 => "Method Not Allowed",
        409 => "Conflict",
        422 => "Unprocessable Entity",
        429 => "Too Many Requests",
        500 => "Internal Server Error",
        _ => $"HTTP {statusCode}"
    };
}
