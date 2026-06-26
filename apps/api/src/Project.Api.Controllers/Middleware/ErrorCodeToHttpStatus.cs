using Project.Application.Common;

namespace Project.Api.Controllers.Middleware;

/// <summary>
/// Internal lookup table mapping Application <see cref="Error.Code"/> values to HTTP status codes.
/// Reserved codes (<c>AUTH_USER_BLOCKED</c>, <c>AUTH_TOKEN_REVOKED</c>) are included for
/// future-proofing but are not emitted by current endpoints.
///
/// Exposed to test assemblies via <c>InternalsVisibleTo</c> so table-driven tests can
/// verify the mapping without making this helper public.
/// </summary>
internal static class ErrorCodeToHttpStatus
{
    /// <summary>
    /// Returns the HTTP status code for the given error code.
    /// Unknown or null codes return 500 (Internal Server Error).
    /// </summary>
    public static int GetStatusCode(string? code)
    {
        if (code is null)
            return StatusCodes.Status500InternalServerError;

        return code switch
        {
            ErrorCodes.Auth.InvalidCredentials or
            ErrorCodes.Auth.TokenExpired or
            ErrorCodes.Auth.TokenReuseDetected or
            ErrorCodes.Auth.UserBlocked or
            ErrorCodes.Auth.TokenRevoked
                => StatusCodes.Status401Unauthorized,

            ErrorCodes.Auth.RefreshTokenMissing
                => StatusCodes.Status400BadRequest,

            ErrorCodes.General.ValidationError
                => StatusCodes.Status400BadRequest,

            ErrorCodes.General.NotFound
                => StatusCodes.Status404NotFound,

            ErrorCodes.General.Conflict
                => StatusCodes.Status409Conflict,

            _ => StatusCodes.Status500InternalServerError
        };
    }
}
