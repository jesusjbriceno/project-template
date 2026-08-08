using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Project.Api.Controllers.Middleware;

namespace Project.UnitTests.Middleware;

/// <summary>
/// Unit tests for <see cref="FrameworkStatusCodePages"/>.
/// Verifies 404/405 mapping to stable codes and the no-double-write guard.
/// </summary>
public sealed class FrameworkStatusCodePagesTests
{
    // ── Constants ──

    [Fact]
    public void RoutingNotFound_HasExpectedValue()
    {
        Assert.Equal("ROUTING_NOT_FOUND", FrameworkStatusCodePages.RoutingNotFound);
    }

    [Fact]
    public void MethodNotAllowed_HasExpectedValue()
    {
        Assert.Equal("METHOD_NOT_ALLOWED", FrameworkStatusCodePages.MethodNotAllowed);
    }

    // ── 404 mapping ──

    [Fact]
    public async Task WriteAsync_404_WritesProblemDetailsWithRoutingNotFoundCode()
    {
        // ARRANGE
        var (context, body) = CreateContext(statusCode: 404);

        // ACT
        await FrameworkStatusCodePages.WriteAsync(CreateStatusCodeContext(context));

        // ASSERT
        Assert.Equal(404, context.Response.StatusCode);
        var raw = GetBodyString(body);
        Assert.Contains("ROUTING_NOT_FOUND", raw);
        Assert.Contains("application/problem+json", context.Response.ContentType);
    }

    // ── 405 mapping ──

    [Fact]
    public async Task WriteAsync_405_WritesProblemDetailsWithMethodNotAllowedCode()
    {
        // ARRANGE
        var (context, body) = CreateContext(statusCode: 405);

        // ACT
        await FrameworkStatusCodePages.WriteAsync(CreateStatusCodeContext(context));

        // ASSERT
        Assert.Equal(405, context.Response.StatusCode);
        var raw = GetBodyString(body);
        Assert.Contains("METHOD_NOT_ALLOWED", raw);
        Assert.Contains("application/problem+json", context.Response.ContentType);
    }

    // ── No double-write guard ──

    [Fact]
    public async Task WriteAsync_ContentTypeAlreadySet_DoesNotWrite()
    {
        // ARRANGE — Content-Type already set by downstream middleware
        var (context, body) = CreateContext(statusCode: 404);
        context.Response.ContentType = "application/json";

        // ACT
        await FrameworkStatusCodePages.WriteAsync(CreateStatusCodeContext(context));

        // ASSERT — body is empty (our handler did not write)
        Assert.Equal(0, body.Length);
    }

    [Fact]
    public async Task WriteAsync_ResponseAlreadyOwned_PreservesOriginalProblemDetailsAndCode()
    {
        // ARRANGE — downstream middleware already wrote a ProblemDetails with a custom code
        var (context, body) = CreateContext(statusCode: 404);
        context.Response.ContentType = "application/problem+json";

        var originalBody = """{"status":404,"code":"CUSTOM_NOT_FOUND","detail":"Custom detail"}""";
        using var writer = new StreamWriter(body, leaveOpen: true);
        await writer.WriteAsync(originalBody);
        await writer.FlushAsync();

        // ACT
        await FrameworkStatusCodePages.WriteAsync(CreateStatusCodeContext(context));

        // ASSERT — body is unchanged (our handler did not overwrite)
        body.Position = 0;
        using var reader = new StreamReader(body);
        var preservedBody = await reader.ReadToEndAsync();
        Assert.Equal(originalBody, preservedBody);

        // ASSERT — original code is preserved (not overwritten with ROUTING_NOT_FOUND)
        Assert.Contains("CUSTOM_NOT_FOUND", preservedBody, StringComparison.Ordinal);
        Assert.DoesNotContain("ROUTING_NOT_FOUND", preservedBody, StringComparison.Ordinal);
    }

    // ── Unrelated status codes are not handled ──

    [Fact]
    public async Task WriteAsync_Non404Or405_DoesNotWrite()
    {
        // ARRANGE
        var (context, body) = CreateContext(statusCode: 500);

        // ACT
        await FrameworkStatusCodePages.WriteAsync(CreateStatusCodeContext(context));

        // ASSERT — body is empty (handler only handles 404/405)
        Assert.Equal(0, body.Length);
    }

    // ── Helpers ──

    private static StatusCodeContext CreateStatusCodeContext(DefaultHttpContext context)
    {
        return new StatusCodeContext(context, new StatusCodePagesOptions(), _ => Task.CompletedTask);
    }

    private static (DefaultHttpContext context, MemoryStream body) CreateContext(int statusCode)
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddProblemDetails();
        var provider = services.BuildServiceProvider();

        var context = new DefaultHttpContext
        {
            RequestServices = provider
        };
        context.Response.StatusCode = statusCode;

        var body = new MemoryStream();
        context.Response.Body = body;

        return (context, body);
    }

    private static string GetBodyString(MemoryStream body)
    {
        body.Position = 0;
        using var reader = new StreamReader(body);
        return reader.ReadToEnd();
    }
}
