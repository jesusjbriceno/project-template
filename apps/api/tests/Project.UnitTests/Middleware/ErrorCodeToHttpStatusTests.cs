using Project.Api.Controllers.Middleware;

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
    [InlineData("AUTH_INVALID_CREDENTIALS", 401)]
    [InlineData("AUTH_TOKEN_EXPIRED", 401)]
    [InlineData("AUTH_TOKEN_REUSE_DETECTED", 401)]
    [InlineData("AUTH_REFRESH_TOKEN_MISSING", 400)]
    [InlineData("VALIDATION_ERROR", 400)]
    [InlineData("NOT_FOUND", 404)]
    [InlineData("CONFLICT", 409)]
    public void GetStatusCode_ActiveAuthCodes_ReturnsExpected(string errorCode, int expectedStatus)
    {
        var status = ErrorCodeToHttpStatus.GetStatusCode(errorCode);

        Assert.Equal(expectedStatus, status);
    }

    // ── Reserved / future-proof codes ────────────────────────

    [Theory]
    [InlineData("AUTH_USER_BLOCKED", 401)]
    [InlineData("AUTH_TOKEN_REVOKED", 401)]
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
