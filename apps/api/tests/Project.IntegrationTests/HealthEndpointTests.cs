using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Project.IntegrationTests;

/// <summary>
/// Integration tests for the /health endpoint.
/// Demonstrates TDD: first test asserts basic availability,
/// second test asserts the health response body is the expected
/// "Healthy" string from the built-in health check service.
/// </summary>
public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_Health_Returns200Ok()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_Health_ReturnsHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        // Assert — the default health check response is "Healthy"
        Assert.Equal("Healthy", content);
    }
}
