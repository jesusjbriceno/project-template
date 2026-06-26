using Microsoft.AspNetCore.Mvc;
using Project.Api.Controllers.Middleware;
using Project.Application.Common;

namespace Project.UnitTests.Middleware;

/// <summary>
/// Unit tests for <see cref="ResultProblemDetailsMapper"/> — verifies that
/// Application <see cref="Result"/> and <see cref="Result{T}"/> failures are
/// mapped to RFC 7807 <see cref="ProblemDetails"/> responses with the correct
/// HTTP status code, title, and <c>extensions.code</c>.
/// </summary>
public sealed class ResultProblemDetailsMapperTests
{
    [Fact]
    public void Map_Result_Success_ThrowsInvalidOperationException()
    {
        var result = Result.Success();

        Assert.Throws<InvalidOperationException>(() => ResultProblemDetailsMapper.Map(result));
    }

    [Fact]
    public void Map_ResultT_Success_ThrowsInvalidOperationException()
    {
        var result = Result<int>.Success(42);

        Assert.Throws<InvalidOperationException>(() => ResultProblemDetailsMapper.Map(result));
    }

    // ── Result (non-generic) ─────────────────────────────────

    [Fact]
    public void Map_Result_AuthInvalidCredentials_Returns401ProblemDetails()
    {
        var result = Result.Failure(ErrorCodes.Auth.InvalidCredentials, "Invalid credentials.");

        var actionResult = ResultProblemDetailsMapper.Map(result);

        AssertProblemDetails(actionResult, expectedStatus: 401, expectedCode: ErrorCodes.Auth.InvalidCredentials);
    }

    [Fact]
    public void Map_Result_AuthTokenExpired_Returns401ProblemDetails()
    {
        var result = Result.Failure(ErrorCodes.Auth.TokenExpired, "Token has expired.");

        var actionResult = ResultProblemDetailsMapper.Map(result);

        AssertProblemDetails(actionResult, expectedStatus: 401, expectedCode: ErrorCodes.Auth.TokenExpired);
    }

    [Fact]
    public void Map_Result_AuthRefreshTokenMissing_Returns400ProblemDetails()
    {
        var result = Result.Failure(ErrorCodes.Auth.RefreshTokenMissing, "Refresh token is required.");

        var actionResult = ResultProblemDetailsMapper.Map(result);

        AssertProblemDetails(actionResult, expectedStatus: 400, expectedCode: ErrorCodes.Auth.RefreshTokenMissing);
    }

    [Fact]
    public void Map_Result_AuthInvalidCredentials_PreservesGenericDetail()
    {
        var result = Result.Failure(ErrorCodes.Auth.InvalidCredentials, "Invalid credentials.");

        var actionResult = ResultProblemDetailsMapper.Map(result);

        var problemDetails = GetProblemDetails(actionResult);
        Assert.Equal("Invalid credentials.", problemDetails.Detail);
        Assert.Equal("https://httpstatuses.com/401", problemDetails.Type);
    }

    [Fact]
    public void Map_Result_UnknownCode_ReturnsSafeGeneric500ProblemDetails()
    {
        var result = Result.Failure("SOME_UNKNOWN_CODE", "Something went wrong.");

        var actionResult = ResultProblemDetailsMapper.Map(result);

        var problemDetails = GetProblemDetails(actionResult);
        Assert.Equal(500, problemDetails.Status);
        Assert.Equal("Internal Server Error", problemDetails.Title);
        Assert.Equal("https://httpstatuses.com/500", problemDetails.Type);
        Assert.Equal("An unexpected error occurred. Please try again later.", problemDetails.Detail);
        Assert.Equal("SOME_UNKNOWN_CODE", problemDetails.Extensions["code"]?.ToString());
    }

    // ── Result<T> (generic) ──────────────────────────────────

    [Fact]
    public void Map_ResultT_AuthTokenReuseDetected_Returns401ProblemDetails()
    {
        var result = Result<string>.Failure(ErrorCodes.Auth.TokenReuseDetected, "Token reuse detected.");

        var actionResult = ResultProblemDetailsMapper.Map(result);

        AssertProblemDetails(actionResult, expectedStatus: 401, expectedCode: ErrorCodes.Auth.TokenReuseDetected);
    }

    [Fact]
    public void Map_ResultT_ValidationError_Returns400ProblemDetails()
    {
        var result = Result<int>.Failure(ErrorCodes.General.ValidationError, "Validation failed.");

        var actionResult = ResultProblemDetailsMapper.Map(result);

        AssertProblemDetails(actionResult, expectedStatus: 400, expectedCode: ErrorCodes.General.ValidationError);
    }

    // ── Safe detail ──────────────────────────────────────────

    /// <summary>
    /// The mapper passes the error message through as detail. The caller
    /// (controller) is responsible for providing a safe, generic message
    /// for auth errors to prevent user enumeration.
    /// </summary>
    [Fact]
    public void Map_Result_DetailPreservesErrorMessage()
    {
        var result = Result.Failure(ErrorCodes.General.Conflict, "The resource already exists.");

        var actionResult = ResultProblemDetailsMapper.Map(result);

        var problemDetails = GetProblemDetails(actionResult);
        Assert.Equal("The resource already exists.", problemDetails.Detail);
        Assert.Equal("https://httpstatuses.com/409", problemDetails.Type);
    }

    // ── Extensions.code present ──────────────────────────────

    [Fact]
    public void Map_Result_ExtensionsContainsCode()
    {
        var result = Result.Failure(ErrorCodes.General.NotFound, "Resource not found.");

        var actionResult = ResultProblemDetailsMapper.Map(result);

        var problemDetails = GetProblemDetails(actionResult);
        Assert.True(problemDetails.Extensions.ContainsKey("code"));
        Assert.Equal(ErrorCodes.General.NotFound, problemDetails.Extensions["code"]?.ToString());
    }

    // ── Title matches HTTP status reason phrase ──────────────

    [Theory]
    [InlineData(ErrorCodes.Auth.InvalidCredentials, 401, "Unauthorized")]
    [InlineData(ErrorCodes.Auth.RefreshTokenMissing, 400, "Bad Request")]
    [InlineData(ErrorCodes.General.NotFound, 404, "Not Found")]
    [InlineData(ErrorCodes.General.Conflict, 409, "Conflict")]
    public void Map_Result_TitleMatchesStatusReason(string code, int expectedStatus, string expectedTitle)
    {
        var result = Result.Failure(code, "Test message.");

        var actionResult = ResultProblemDetailsMapper.Map(result);

        var problemDetails = GetProblemDetails(actionResult);
        Assert.Equal(expectedStatus, problemDetails.Status);
        Assert.Equal(expectedTitle, problemDetails.Title);
    }

    // ── Helpers ───────────────────────────────────────────────

    private static void AssertProblemDetails(IActionResult actionResult, int expectedStatus, string expectedCode)
    {
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(expectedStatus, objectResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(expectedStatus, problemDetails.Status);
        Assert.NotNull(problemDetails.Title);
        Assert.NotNull(problemDetails.Type);
        Assert.True(problemDetails.Extensions.ContainsKey("code"));
        Assert.Equal(expectedCode, problemDetails.Extensions["code"]?.ToString());
    }

    private static ProblemDetails GetProblemDetails(IActionResult actionResult)
    {
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        return Assert.IsType<ProblemDetails>(objectResult.Value);
    }
}
