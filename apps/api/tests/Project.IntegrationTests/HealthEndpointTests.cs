extern alias ApiControllers;

using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

// ApiControllers::Program resolves the CS0433 ambiguity caused by both
// Project.Api.Controllers and Project.MigrationService exposing a Program type.
using ApiProgram = ApiControllers::Program;

namespace Project.IntegrationTests;

/// <summary>
/// Integration tests for the /health endpoint.
/// Demonstrates TDD: first test asserts basic availability,
/// second test asserts the health response body is the expected
/// "Healthy" string from the built-in health check service.
/// </summary>
public sealed class HealthEndpointTests : IClassFixture<HealthWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(HealthWebApplicationFactory factory)
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
