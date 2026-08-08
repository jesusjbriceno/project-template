extern alias ApiControllers;

using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using ApiProgram = ApiControllers::Program;

namespace Project.IntegrationTests.Auth;

/// <summary>
/// Runtime evidence tests for AuthController response metadata.
/// Reflects on [ProducesResponseType] attributes and compares with the OpenAPI document.
/// </summary>
public sealed class AuthResponseContractTests : IClassFixture<AuthTestFixture>
{
    private readonly AuthTestFixture _fixture;

    public AuthResponseContractTests(AuthTestFixture fixture)
    {
        _fixture = fixture;
    }

    private static Type GetControllerType()
    {
        var type = typeof(ApiProgram).Assembly.GetType("Project.Api.Controllers.Controllers.AuthController");
        return type ?? throw new InvalidOperationException("AuthController not found");
    }

    private static Type GetTokenResponseType()
    {
        var type = typeof(ApiProgram).Assembly.GetType("Project.Api.Controllers.Contracts.TokenResponse");
        return type ?? throw new InvalidOperationException("TokenResponse not found");
    }

    // ── Login metadata ──

    [Fact]
    public void LoginAction_HasRequiredProducesResponseTypeAttributes()
    {
        var controllerType = GetControllerType();
        var tokenResponseType = GetTokenResponseType();
        var method = controllerType.GetMethod("Login");
        Assert.NotNull(method);

        var attributes = method!.GetCustomAttributes<ProducesResponseTypeAttribute>().ToList();

        Assert.Contains(attributes, a => a.StatusCode == 200 && a.Type == tokenResponseType);
        Assert.Contains(attributes, a => a.StatusCode == 400 && a.Type == typeof(ProblemDetails));
        Assert.Contains(attributes, a => a.StatusCode == 401 && a.Type == typeof(ProblemDetails));
    }

    // ── Refresh and Logout metadata ──

    [Fact]
    public void RefreshAction_HasRequiredProducesResponseTypeAttributes()
    {
        var controllerType = GetControllerType();
        var tokenResponseType = GetTokenResponseType();
        var method = controllerType.GetMethod("Refresh");
        Assert.NotNull(method);

        var attributes = method!.GetCustomAttributes<ProducesResponseTypeAttribute>().ToList();

        Assert.Contains(attributes, a => a.StatusCode == 200 && a.Type == tokenResponseType);
        Assert.Contains(attributes, a => a.StatusCode == 400 && a.Type == typeof(ProblemDetails));
        Assert.Contains(attributes, a => a.StatusCode == 401 && a.Type == typeof(ProblemDetails));
    }

    [Fact]
    public void LogoutAction_HasRequiredProducesResponseTypeAttributes()
    {
        var controllerType = GetControllerType();
        var method = controllerType.GetMethod("Logout");
        Assert.NotNull(method);

        var attributes = method!.GetCustomAttributes<ProducesResponseTypeAttribute>().ToList();

        Assert.Contains(attributes, a => a.StatusCode == 204);
        Assert.Contains(attributes, a => a.StatusCode == 400 && a.Type == typeof(ProblemDetails));
    }

    // ── OpenAPI document advertises login response types ──

    [Fact]
    public async Task OpenApiDocument_LoginOperation_AdvertisesExpectedResponseTypes()
    {
        // ARRANGE
        using var client = _fixture.CreateClient();

        // ACT
        var response = await client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();
        var rawJson = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(rawJson);

        // ASSERT — POST /auth/login operation exists
        var loginOp = doc.RootElement
            .GetProperty("paths")
            .GetProperty("/auth/login")
            .GetProperty("post");

        var responses = loginOp.GetProperty("responses");

        // ASSERT — 200 response declared with TokenResponse schema
        Assert.True(responses.TryGetProperty("200", out var ok200),
            "POST /auth/login must declare a 200 response");
        AssertSchemaReference(ok200, "TokenResponse", "200 response must reference TokenResponse schema");

        // ASSERT — 400 response declared with ProblemDetails schema
        Assert.True(responses.TryGetProperty("400", out var bad400),
            "POST /auth/login must declare a 400 response");
        AssertSchemaReference(bad400, "ProblemDetails", "400 response must reference ProblemDetails schema");

        // ASSERT — 401 response declared with ProblemDetails schema
        Assert.True(responses.TryGetProperty("401", out var unauth401),
            "POST /auth/login must declare a 401 response");
        AssertSchemaReference(unauth401, "ProblemDetails", "401 response must reference ProblemDetails schema");
    }

    private static void AssertSchemaReference(JsonElement responseElement, string expectedSchemaName, string message)
    {
        // Navigate: response -> content -> application/json (or application/problem+json) -> schema -> $ref
        Assert.True(responseElement.TryGetProperty("content", out var content),
            $"{message}: missing content");

        // Try both media types
        JsonElement mediaType;
        if (content.TryGetProperty("application/json", out var appJson))
        {
            mediaType = appJson;
        }
        else if (content.TryGetProperty("application/problem+json", out var problemJson))
        {
            mediaType = problemJson;
        }
        else
        {
            Assert.Fail($"{message}: no application/json or application/problem+json content type found");
            return;
        }

        Assert.True(mediaType.TryGetProperty("schema", out var schema),
            $"{message}: missing schema");

        Assert.True(schema.TryGetProperty("$ref", out var refElement),
            $"{message}: schema must be a $ref");

        var refValue = refElement.GetString();
        Assert.NotNull(refValue);
        Assert.EndsWith($"/schemas/{expectedSchemaName}", refValue, StringComparison.Ordinal);
    }
}
