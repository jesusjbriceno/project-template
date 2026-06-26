using Project.Api.Controllers.Middleware;
using Project.Application.Common;

namespace Project.UnitTests.Middleware;

/// <summary>
/// Unit tests for <see cref="ErrorCodeToHttpStatus"/> — verifies that active and
/// reserved error codes map to the correct HTTP status codes and that unknown
/// codes default to 500.
/// </summary>
public sealed class ErrorCodeToHttpStatusTests
{
    // ── Active auth codes ────────────────────────────────────

    [Theory]
    [InlineData(ErrorCodes.Auth.InvalidCredentials, 401)]
    [InlineData(ErrorCodes.Auth.TokenExpired, 401)]
    [InlineData(ErrorCodes.Auth.TokenReuseDetected, 401)]
    [InlineData(ErrorCodes.Auth.RefreshTokenMissing, 400)]
    [InlineData(ErrorCodes.General.ValidationError, 400)]
    [InlineData(ErrorCodes.General.NotFound, 404)]
    [InlineData(ErrorCodes.General.Conflict, 409)]
    public void GetStatusCode_ActiveCodes_ReturnsExpected(string errorCode, int expectedStatus)
    {
        var status = ErrorCodeToHttpStatus.GetStatusCode(errorCode);

        Assert.Equal(expectedStatus, status);
    }

    // ── Reserved / future-proof codes ────────────────────────

    [Theory]
    [InlineData(ErrorCodes.Auth.UserBlocked, 401)]
    [InlineData(ErrorCodes.Auth.TokenRevoked, 401)]
    public void GetStatusCode_ReservedAuthCodes_ReturnsExpected(string errorCode, int expectedStatus)
    {
        var status = ErrorCodeToHttpStatus.GetStatusCode(errorCode);

        Assert.Equal(expectedStatus, status);
    }

    // ── Unknown codes ────────────────────────────────────────

    [Theory]
    [InlineData("UNKNOWN_CODE")]
    [InlineData("SOMETHING_UNMAPPED")]
    [InlineData("")]
    public void GetStatusCode_UnknownCodes_Returns500(string errorCode)
    {
        var status = ErrorCodeToHttpStatus.GetStatusCode(errorCode);

        Assert.Equal(500, status);
    }

    // ── Null code ────────────────────────────────────────────

    [Fact]
    public void GetStatusCode_NullCode_Returns500()
    {
        var status = ErrorCodeToHttpStatus.GetStatusCode(null!);

        Assert.Equal(500, status);
    }
}
