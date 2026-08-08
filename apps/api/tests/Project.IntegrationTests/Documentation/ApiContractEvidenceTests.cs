extern alias ApiControllers;

using System.Text.Json;
using Xunit;
using ApiProgram = ApiControllers::Program;

namespace Project.IntegrationTests.Documentation;

/// <summary>
/// Runtime evidence tests for API documentation scenarios:
/// enum serialization, XML schema description propagation, and .http smoke document.
/// </summary>
public sealed class ApiContractEvidenceTests : IClassFixture<Auth.AuthTestFixture>
{
    private readonly Auth.AuthTestFixture _fixture;

    public ApiContractEvidenceTests(Auth.AuthTestFixture fixture)
    {
        _fixture = fixture;
    }

    // ── Enum serialization ──

    [Fact]
    public async Task EnumEndpoint_ReturnsStringName_NotInteger()
    {
        // ARRANGE
        using var client = _fixture.CreateClient();

        // ACT
        var response = await client.GetAsync("/test-enum/sample");
        var rawJson = await response.Content.ReadAsStringAsync();

        // ASSERT — enum appears as string name, not integer
        Assert.Contains("\"Active\"", rawJson);
        Assert.DoesNotContain(":1", rawJson);
        Assert.DoesNotContain(": 1", rawJson);

        // ASSERT — round-trip: deserialize back to typed enum
        var doc = JsonDocument.Parse(rawJson);
        var statusValue = doc.RootElement.GetProperty("status").GetString();
        Assert.Equal("Active", statusValue);
        Assert.True(Enum.TryParse<Documentation.TestEnum>(statusValue, out var parsed));
        Assert.Equal(Documentation.TestEnum.Active, parsed);
    }

    // ── XML schema description ──

    [Fact]
    public async Task OpenApiSchema_TokenResponse_HasNonEmptyDescription()
    {
        // ARRANGE
        using var client = _fixture.CreateClient();

        // ACT
        var response = await client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();
        var rawJson = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(rawJson);

        // ASSERT — TokenResponse schema has a non-empty description
        var schemas = doc.RootElement
            .GetProperty("components")
            .GetProperty("schemas");

        Assert.True(schemas.TryGetProperty("TokenResponse", out var tokenSchema),
            "TokenResponse schema must exist in OpenAPI document");

        Assert.True(tokenSchema.TryGetProperty("description", out var description),
            "TokenResponse schema must have a description property");

        var descriptionText = description.GetString();
        Assert.False(string.IsNullOrWhiteSpace(descriptionText),
            "TokenResponse schema description must be non-empty");

        // ASSERT — description contains the expected XML <summary> substring
        Assert.Contains("Response body for successful auth operations", descriptionText, StringComparison.Ordinal);
        Assert.Contains("JWT access token", descriptionText, StringComparison.Ordinal);
    }

    // ── .http smoke document ──

    [Fact]
    public void SmokeHttpFile_ContainsAuthEndpointsAndBaseUrlVariants()
    {
        // ARRANGE — find the repository root (api-smoke.http is at repo root)
        var repoRoot = FindRepoRoot();
        var smokeFilePath = Path.Combine(repoRoot, "api-smoke.http");
        Assert.True(File.Exists(smokeFilePath), "api-smoke.http must exist at repository root");

        // ACT
        var content = File.ReadAllText(smokeFilePath);

        // ASSERT — auth endpoints referenced
        Assert.Contains("POST {{baseUrl}}/auth/login", content);
        Assert.Contains("POST {{baseUrl}}/auth/refresh", content);
        Assert.Contains("POST {{baseUrl}}/auth/logout", content);

        // ASSERT — @baseUrl declared
        Assert.Contains("@baseUrl", content);

        // ASSERT — Docker Compose and dotnet run variants present (commented)
        Assert.Contains("localhost:8080", content);
        Assert.Contains("dotnet run", content, StringComparison.OrdinalIgnoreCase);
    }

    // ── Helpers ──

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "api-smoke.http")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new InvalidOperationException("Could not find repository root containing api-smoke.http");
    }
}
