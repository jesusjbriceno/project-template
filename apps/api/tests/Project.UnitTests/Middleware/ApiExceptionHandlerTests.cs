using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Project.Api.Controllers.Middleware;

namespace Project.UnitTests.Middleware;

public sealed class ApiExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ClientCancellation_ReturnsTrueAndSuppressesProblemDetails()
    {
        var httpContext = CreateHttpContext();
        var handler = CreateHandler();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        httpContext.RequestAborted = cts.Token;

        var handled = await handler.TryHandleAsync(
            httpContext,
            new OperationCanceledException("Client disconnected."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.False(httpContext.Response.HasStarted);
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_ServerSideCancellation_ReturnsSafe500ProblemDetails()
    {
        var httpContext = CreateHttpContext();
        var handler = CreateHandler();

        var handled = await handler.TryHandleAsync(
            httpContext,
            new TaskCanceledException("Timeout expired."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);

        httpContext.Response.Body.Position = 0;
        var problemDetails = await System.Text.Json.JsonSerializer.DeserializeAsync<ProblemDetails>(httpContext.Response.Body);

        Assert.NotNull(problemDetails);
        Assert.Equal(500, problemDetails!.Status);
        Assert.Equal("Internal Server Error", problemDetails.Title);
        Assert.Equal("https://httpstatuses.com/500", problemDetails.Type);
        Assert.Equal("An unexpected error occurred. Please try again later.", problemDetails.Detail);
    }

    [Fact]
    public async Task TryHandleAsync_ResponseAlreadyStarted_ReturnsFalse()
    {
        var httpContext = CreateHttpContext();
        var handler = CreateHandler();
        httpContext.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());

        Assert.True(httpContext.Response.HasStarted);

        var handled = await handler.TryHandleAsync(
            httpContext,
            new InvalidOperationException("Boom."),
            CancellationToken.None);

        Assert.False(handled);
    }

    private static ApiExceptionHandler CreateHandler() =>
        new(NullLogger<ApiExceptionHandler>.Instance, new TestProblemDetailsService());

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        return context;
    }

    private sealed class TestProblemDetailsService : IProblemDetailsService
    {
        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            context.HttpContext.Response.ContentType = "application/problem+json";
            return new ValueTask(context.HttpContext.Response.WriteAsJsonAsync(context.ProblemDetails));
        }
    }

    private sealed class StartedResponseFeature : IHttpResponseFeature
    {
        public int StatusCode { get; set; }

        public string? ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        public Stream Body { get; set; } = Stream.Null;

        public bool HasStarted => true;

        public void OnCompleted(Func<object, Task> callback, object state) { }

        public void OnStarting(Func<object, Task> callback, object state) { }
    }
}
