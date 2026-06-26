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
        if (result.IsSuccess)
            throw new InvalidOperationException("A successful Result cannot be mapped to ProblemDetails.");

        var statusCode = ErrorCodeToHttpStatus.GetStatusCode(result.Error.Code);
        var problemDetails = ProblemDetailsResponseFactory.Create(statusCode, result.Error.Code, result.Error.Message);

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }

    /// <summary>
    /// Maps a failed <see cref="Result{T}"/> to an <see cref="IActionResult"/> with
    /// a <see cref="ProblemDetails"/> body, preserving the error code in
    /// <c>extensions.code</c>.
    /// </summary>
    public static IActionResult Map<T>(Result<T> result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("A successful Result cannot be mapped to ProblemDetails.");

        var statusCode = ErrorCodeToHttpStatus.GetStatusCode(result.Error.Code);
        var problemDetails = ProblemDetailsResponseFactory.Create(statusCode, result.Error.Code, result.Error.Message);

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }
}
