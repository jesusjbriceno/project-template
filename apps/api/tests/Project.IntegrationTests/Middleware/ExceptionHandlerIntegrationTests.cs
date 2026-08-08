extern alias ApiControllers;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using ApiProgram = ApiControllers::Program;

namespace Project.IntegrationTests.Middleware;

/// <summary>
/// Integration tests for the global exception handler and status-code pipeline.
/// Verifies that unhandled exceptions return safe 500 ProblemDetails and that
/// framework-generated status codes (404) produce ProblemDetails bodies.
/// </summary>
public sealed class ExceptionHandlerIntegrationTests : IClassFixture<Auth.AuthTestFixture>
{
    private readonly Auth.AuthTestFixture _fixture;

    public ExceptionHandlerIntegrationTests(Auth.AuthTestFixture fixture)
    {
        _fixture = fixture;
    }

    // ── Unhandled exception → 500 ProblemDetails ─────────────

    /// <summary>
    /// Task 1.2: An unhandled exception in any controller action must produce a
    /// safe 500 ProblemDetails response — no stack trace, no internal message.
    /// </summary>
    [Fact]
    public async Task UnhandledException_Returns500ProblemDetailsSafeDetail()
    {
        // ARRANGE
        using var client = _fixture.CreateClient();

        // ACT — calls the test-only throwing endpoint
        var response = await client.GetAsync("/test-throw/unhandled");

        // ASSERT — status code
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        // ASSERT — body is ProblemDetails
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Equal(500, problemDetails!.Status);
        Assert.NotNull(problemDetails.Title);

        // ASSERT — safe detail: does NOT leak internal exception message
        Assert.Equal("An unexpected error occurred. Please try again later.", problemDetails.Detail);
        Assert.DoesNotContain("Test exception", problemDetails.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InvalidOperationException", problemDetails.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("https://httpstatuses.com/500", problemDetails.Type);

        // ASSERT — no stack trace leaked
        var rawBody = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("stackTrace", rawBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("   at ", rawBody, StringComparison.Ordinal);
    }

    // ── 404 from routing → ProblemDetails ────────────────────

    /// <summary>
    /// Task 1.3: A 404 from the routing middleware must produce a ProblemDetails
    /// body, not an empty or HTML body.
    /// </summary>
    [Fact]
    public async Task NotFoundRoute_Returns404ProblemDetails()
    {
        // ARRANGE
        using var client = _fixture.CreateClient();

        // ACT — GET a route that does not exist
        var response = await client.GetAsync("/nonexistent-route-xyz");

        // ASSERT
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Equal(404, problemDetails!.Status);

        // ASSERT — stable public code
        Assert.Equal("ROUTING_NOT_FOUND", ((JsonElement)problemDetails.Extensions["code"]!).GetString());

        // ASSERT — generic detail: no route, no stack, no internal exception
        Assert.NotNull(problemDetails.Detail);
        Assert.DoesNotContain("/nonexistent-route-xyz", problemDetails.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stack", problemDetails.Detail, StringComparison.OrdinalIgnoreCase);

        // ASSERT — no leakage of internals in raw body: no file path, line number, exception type, or stack frame
        var rawBody = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("/nonexistent-route-xyz", rawBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("   at ", rawBody, StringComparison.Ordinal);
        Assert.DoesNotContain(".cs:", rawBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("line ", rawBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exception", rawBody, StringComparison.OrdinalIgnoreCase);
    }

    // ── 405 method not allowed → ProblemDetails ──────────────

    /// <summary>
    /// Task 1.3: A 405 Method Not Allowed must produce a ProblemDetails body.
    /// </summary>
    [Fact]
    public async Task MethodNotAllowed_Returns405ProblemDetails()
    {
        // ARRANGE
        using var client = _fixture.CreateClient();

        // ACT — DELETE a route that only accepts GET (test-auth/protected)
        var response = await client.DeleteAsync("/test-auth/protected");

        // ASSERT — 405 with ProblemDetails body (not empty)
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Equal(405, problemDetails!.Status);

        // ASSERT — stable public code
        Assert.Equal("METHOD_NOT_ALLOWED", ((JsonElement)problemDetails.Extensions["code"]!).GetString());

        // ASSERT — generic detail
        Assert.NotNull(problemDetails.Detail);
        Assert.DoesNotContain("stack", problemDetails.Detail, StringComparison.OrdinalIgnoreCase);

        // ASSERT — no leakage of internals in raw body: no file path, line number, exception type, or stack frame
        var rawBody405 = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("   at ", rawBody405, StringComparison.Ordinal);
        Assert.DoesNotContain(".cs:", rawBody405, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("line ", rawBody405, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exception", rawBody405, StringComparison.OrdinalIgnoreCase);

        // ASSERT — Allow header preserved (content-level header in HttpResponseMessage)
        Assert.True(response.Content.Headers.Contains("Allow"), "405 response must include Allow header");
    }

    // ── Client disconnect (cancellation) → no 500 ??
    // (verification deferred: the handler must ignore request-aborted cancellations)
}
