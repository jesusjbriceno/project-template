extern alias ApiControllers;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using ApiProgram = ApiControllers::Program;

namespace Project.IntegrationTests.Auth;

/// <summary>
/// Integration tests for JWT authentication middleware.
/// Verifies that protected endpoints return 401 when no valid access token is provided.
///
/// Uses a test-only <c>/test-auth/protected</c> endpoint (registered via
/// <see cref="AuthWebApplicationFactory"/>) rather than a production endpoint,
/// so the production API surface is not expanded for testing purposes.
/// </summary>
public sealed class AuthMiddlewareTests : IClassFixture<AuthTestFixture>
{
    private readonly AuthTestFixture _fixture;

    public AuthMiddlewareTests(AuthTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Task 3.4: Request to a protected endpoint without any token → 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task NoToken_Returns401()
    {
        // ACT — GET /test-auth/protected without any Authorization header
        using var client = _fixture.CreateClient();
        var response = await client.GetAsync("/test-auth/protected");

        // ASSERT
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Task 3.4: Request with an invalid (malformed) access token → 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task InvalidToken_Returns401()
    {
        // ARRANGE — set a clearly invalid token
        using var client = _fixture.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/test-auth/protected");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "this-is-not-a-valid-jwt");

        // ACT
        var response = await client.SendAsync(request);

        // ASSERT
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Task 3.4: Request with a valid access token (obtained via login) → 200 OK.
    /// Proves the middleware flow works end-to-end: token issuance → validation → authorized access.
    /// </summary>
    [Fact]
    public async Task ValidToken_AllowsAccess()
    {
        // ARRANGE — login to get a valid access token
        using var client = _fixture.CreateClient();
        var loginPayload = new { email = AuthTestFixture.TestUserEmail, password = AuthTestFixture.TestUserPassword };
        var loginResponse = await client.PostAsJsonAsync("/auth/login", loginPayload);
        loginResponse.EnsureSuccessStatusCode();

        var body = await loginResponse.Content.ReadFromJsonAsync<TokenResponseContract>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));

        // ACT — call protected endpoint with the valid token
        var request = new HttpRequestMessage(HttpMethod.Get, "/test-auth/protected");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);

        var response = await client.SendAsync(request);

        // ASSERT
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Task 3.4: Request with a tampered access token → 401 Unauthorized.
    /// Modifies the last character of a valid JWT so the signature no longer matches.
    /// </summary>
    [Fact]
    public async Task TamperedToken_Returns401()
    {
        // ARRANGE — login to get a valid access token, then tamper with it
        using var client = _fixture.CreateClient();
        var loginPayload = new { email = AuthTestFixture.TestUserEmail, password = AuthTestFixture.TestUserPassword };
        var loginResponse = await client.PostAsJsonAsync("/auth/login", loginPayload);
        loginResponse.EnsureSuccessStatusCode();

        var body = await loginResponse.Content.ReadFromJsonAsync<TokenResponseContract>();
        Assert.NotNull(body);

        // Tamper with the last character of the token → invalid signature → 401
        var tamperedToken = body!.AccessToken[..^1] + (body.AccessToken[^1] == 'A' ? 'B' : 'A');

        // ACT
        var request = new HttpRequestMessage(HttpMethod.Get, "/test-auth/protected");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);

        var response = await client.SendAsync(request);

        // ASSERT
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Inline contract for deserialization
    private sealed record TokenResponseContract(string AccessToken, int ExpiresIn);
}
