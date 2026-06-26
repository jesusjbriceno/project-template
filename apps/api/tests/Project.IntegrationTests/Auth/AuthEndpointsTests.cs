extern alias ApiControllers;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using ApiProgram = ApiControllers::Program;

namespace Project.IntegrationTests.Auth;

/// <summary>
/// Integration tests for auth endpoints (login, refresh, logout).
///
/// These tests exercise the full HTTP pipeline through <see cref="WebApplicationFactory{ApiProgram}"/>
/// with a real PostgreSQL container via Testcontainers.
///
/// Each test method creates its own <see cref="HttpClient"/> via
/// <see cref="AuthTestFixture.CreateClient()"/>. Clients are configured with
/// <c>HandleCookies = false</c> so the client never automatically stores or
/// retransmits cookies. Tests manually extract <c>Set-Cookie</c> values and
/// pass the exact cookie under test as a <c>Cookie</c> request header — this
/// removes cookie-jar ambiguity, which is essential for rotation/reuse scenarios.
/// </summary>
public sealed class AuthEndpointsTests : IClassFixture<AuthTestFixture>
{
    private readonly AuthTestFixture _fixture;

    public AuthEndpointsTests(AuthTestFixture fixture)
    {
        _fixture = fixture;
    }

    // ─────────────── Login ───────────────

    /// <summary>
    /// Task 3.1: Valid credentials → 200 OK + access token in body + refresh token in HttpOnly cookie.
    /// </summary>
    [Fact]
    public async Task Login_ValidCredentials_Returns200WithAccessTokenAndCookie()
    {
        // ARRANGE
        using var client = _fixture.CreateClient();
        var payload = new { email = AuthTestFixture.TestUserEmail, password = AuthTestFixture.TestUserPassword };

        // ACT
        var response = await client.PostAsJsonAsync("/auth/login", payload);

        // ASSERT — status
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // ASSERT — body contains access token and expiry
        var body = await response.Content.ReadFromJsonAsync<TokenResponseContract>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.True(body.ExpiresIn > 0);

        // ASSERT — Set-Cookie header contains refreshToken cookie with correct security attributes
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();
        Assert.Contains(setCookieHeaders, h => h.StartsWith("refreshToken=", StringComparison.OrdinalIgnoreCase));
        var cookieHeader = setCookieHeaders.First(h => h.StartsWith("refreshToken=", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("HttpOnly", cookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SameSite=Strict", cookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/auth", cookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Max-Age=", cookieHeader, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Task 3.1: Invalid credentials → 401 with generic message (no user enumeration).
    /// </summary>
    [Fact]
    public async Task Login_InvalidCredentials_Returns401Generic()
    {
        // ARRANGE — wrong password
        using var client = _fixture.CreateClient();
        var payload = new { email = AuthTestFixture.TestUserEmail, password = "WrongP@ssw0rd!" };

        // ACT
        var response = await client.PostAsJsonAsync("/auth/login", payload);

        // ASSERT
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(body);
        Assert.Equal(401, body!.Status);
        Assert.Equal("Unauthorized", body.Title);
        // Generic detail — must NOT leak user existence
        Assert.Contains("Invalid", body.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("AUTH_INVALID_CREDENTIALS", ((JsonElement)body.Extensions["code"]!).GetString());

        // ASSERT — no Set-Cookie header
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    /// <summary>
    /// Task 3.1: Non-existent email → same 401 as wrong password (no enumeration).
    /// </summary>
    [Fact]
    public async Task Login_NonExistentUser_Returns401SameMessage()
    {
        // ARRANGE
        using var client = _fixture.CreateClient();
        var payload = new { email = "nobody@nowhere.test", password = "SomeP@ssw0rd!" };

        // ACT
        var response = await client.PostAsJsonAsync("/auth/login", payload);

        // ASSERT
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(body);
        Assert.Equal(401, body!.Status);
        Assert.Equal("Unauthorized", body.Title);
        Assert.Contains("Invalid", body.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("AUTH_INVALID_CREDENTIALS", ((JsonElement)body.Extensions["code"]!).GetString());
    }

    // ─────────────── Refresh ───────────────

    /// <summary>
    /// Task 3.1: Valid refresh cookie → 200 with new access token + rotated refresh cookie.
    /// Asserts that the rotated cookie value DIFFERS from the old cookie value (true rotation).
    /// </summary>
    [Fact]
    public async Task Refresh_ValidCookie_Returns200WithRotatedTokens()
    {
        // ARRANGE — login first to get a valid cookie
        using var client = _fixture.CreateClient();
        var loginPayload = new { email = AuthTestFixture.TestUserEmail, password = AuthTestFixture.TestUserPassword };
        var loginResponse = await client.PostAsJsonAsync("/auth/login", loginPayload);
        loginResponse.EnsureSuccessStatusCode();

        var loginCookies = ExtractCookies(loginResponse);
        Assert.Contains(loginCookies, c => c.Name == "refreshToken");
        var oldRefreshValue = loginCookies.First(c => c.Name == "refreshToken").Value;

        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        refreshRequest.Headers.Add("Cookie", $"refreshToken={oldRefreshValue}");

        // ACT
        var response = await client.SendAsync(refreshRequest);

        // ASSERT — 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TokenResponseContract>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));

        // ASSERT — rotated cookie was set and value differs from old value (CRITICAL 3 rotation proof)
        var newCookies = ExtractCookies(response);
        Assert.Contains(newCookies, c => c.Name == "refreshToken");
        var newRefreshValue = newCookies.First(c => c.Name == "refreshToken").Value;
        Assert.NotEqual(oldRefreshValue, newRefreshValue);

        // ASSERT — cookie path is /auth (CRITICAL 1 browser logout fix)
        var cookieHeader = response.Headers.GetValues("Set-Cookie")
            .First(h => h.StartsWith("refreshToken=", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("path=/auth", cookieHeader, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Task 3.1: Missing cookie → 400 AUTH_REFRESH_TOKEN_MISSING.
    /// </summary>
    [Fact]
    public async Task Refresh_MissingCookie_Returns400()
    {
        // ACT — no cookie sent
        using var client = _fixture.CreateClient();
        var response = await client.PostAsync("/auth/refresh", null);

        // ASSERT
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(body);
        Assert.Equal(400, body!.Status);
        Assert.Equal("Bad Request", body.Title);
        Assert.Equal("AUTH_REFRESH_TOKEN_MISSING", ((JsonElement)body.Extensions["code"]!).GetString());
    }
    [Fact]
    public async Task Refresh_RevokedToken_Returns401()
    {
        // ARRANGE — login to get a token, then logout to revoke the family
        using var client = _fixture.CreateClient();
        var loginPayload = new { email = AuthTestFixture.TestUserEmail, password = AuthTestFixture.TestUserPassword };
        var loginResponse = await client.PostAsJsonAsync("/auth/login", loginPayload);
        loginResponse.EnsureSuccessStatusCode();

        var cookies = ExtractCookies(loginResponse);
        var refreshCookie = cookies.First(c => c.Name == "refreshToken");

        // Logout to revoke the family
        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/logout");
        logoutRequest.Headers.Add("Cookie", $"refreshToken={refreshCookie.Value}");
        await client.SendAsync(logoutRequest);

        // ACT — try to refresh with the now-revoked cookie
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        refreshRequest.Headers.Add("Cookie", $"refreshToken={refreshCookie.Value}");

        var response = await client.SendAsync(refreshRequest);

        // ASSERT — should be 401 (token revoked)
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(body);
        Assert.Equal(401, body!.Status);
        Assert.Equal("Unauthorized", body.Title);
        Assert.True(body.Extensions.ContainsKey("code"));

        // ASSERT — cookie is cleared (CRITICAL 3 + WARNING 5)
        AssertContainsClearSetCookie(response);
    }

    /// <summary>
    /// CRITICAL 3: Reuse after successful rotation → 401 + cookie cleared + family revoked.
    ///
    /// 1. Login → get T1
    /// 2. Refresh with T1 → gets T2 rotated (T1 now revoked)
    /// 3. Try to refresh with T1 again → 401, cookie cleared, family revoked
    /// </summary>
    [Fact]
    public async Task Refresh_ReuseAfterRotation_Returns401AndClearsCookie()
    {
        // ARRANGE — login to get T1
        using var client = _fixture.CreateClient();
        var loginPayload = new { email = AuthTestFixture.TestUserEmail, password = AuthTestFixture.TestUserPassword };
        var loginResponse = await client.PostAsJsonAsync("/auth/login", loginPayload);
        loginResponse.EnsureSuccessStatusCode();

        var loginCookies = ExtractCookies(loginResponse);
        var oldRefreshValue = loginCookies.First(c => c.Name == "refreshToken").Value;

        // Step 2 — successful rotation T1 → T2
        var rotate1Request = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        rotate1Request.Headers.Add("Cookie", $"refreshToken={oldRefreshValue}");
        var rotate1Response = await client.SendAsync(rotate1Request);
        Assert.Equal(HttpStatusCode.OK, rotate1Response.StatusCode);
        // T1 is now revoked, T2 is active

        // Step 3 — present old T1 after rotation → reuse detection → 401
        var reuseRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        reuseRequest.Headers.Add("Cookie", $"refreshToken={oldRefreshValue}");

        var response = await client.SendAsync(reuseRequest);

        // ASSERT — reuse detected, 401 with cookie cleared
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(body);
        Assert.Equal(401, body!.Status);
        Assert.Equal("Unauthorized", body.Title);
        Assert.Equal("AUTH_TOKEN_REUSE_DETECTED", ((JsonElement)body.Extensions["code"]!).GetString());

        // ASSERT — cookie is cleared (family revoked, client should discard all tokens)
        AssertContainsClearSetCookie(response);

        // ASSERT — the rotated token T2 is ALSO invalidated (prove family revocation)
        var postReuseCookies = ExtractCookies(rotate1Response);
        var rotatedToken = postReuseCookies.First(c => c.Name == "refreshToken").Value;
        var tryT2 = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        tryT2.Headers.Add("Cookie", $"refreshToken={rotatedToken}");
        var t2Response = await client.SendAsync(tryT2);
        Assert.Equal(HttpStatusCode.Unauthorized, t2Response.StatusCode);
    }

    // ─────────────── Logout ───────────────

    /// <summary>
    /// Task 3.1: Valid refresh cookie → 204 No Content + cookie cleared (Max-Age=0) + family revoked.
    /// </summary>
    [Fact]
    public async Task Logout_ValidCookie_Returns204AndClearsCookie()
    {
        // ARRANGE — login first
        using var client = _fixture.CreateClient();
        var loginPayload = new { email = AuthTestFixture.TestUserEmail, password = AuthTestFixture.TestUserPassword };
        var loginResponse = await client.PostAsJsonAsync("/auth/login", loginPayload);
        loginResponse.EnsureSuccessStatusCode();

        var cookies = ExtractCookies(loginResponse);
        var refreshCookie = cookies.First(c => c.Name == "refreshToken");

        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/logout");
        logoutRequest.Headers.Add("Cookie", $"refreshToken={refreshCookie.Value}");

        // ACT
        var response = await client.SendAsync(logoutRequest);

        // ASSERT — 204 No Content
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // ASSERT — cookie is cleared (Max-Age=0, path matches so browser replaces)
        AssertContainsClearSetCookie(response);
    }

    /// <summary>
    /// Task 3.1: Missing cookie → 400 AUTH_REFRESH_TOKEN_MISSING.
    /// </summary>
    [Fact]
    public async Task Logout_MissingCookie_Returns400()
    {
        // ACT — no cookie sent
        using var client = _fixture.CreateClient();
        var response = await client.PostAsync("/auth/logout", null);

        // ASSERT
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(body);
        Assert.Equal(400, body!.Status);
        Assert.Equal("Bad Request", body.Title);
        Assert.Equal("AUTH_REFRESH_TOKEN_MISSING", ((JsonElement)body.Extensions["code"]!).GetString());
    }

    // ─────────────── Helpers ───────────────

    private static List<(string Name, string Value)> ExtractCookies(HttpResponseMessage response)
    {
        var cookies = new List<(string, string)>();
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookieValues))
        {
            foreach (var header in setCookieValues)
            {
                var parts = header.Split(';')[0].Split('=', 2);
                if (parts.Length == 2)
                    cookies.Add((parts[0].Trim(), parts[1].Trim()));
            }
        }
        return cookies;
    }

    /// <summary>
    /// Asserts that the response contains a Set-Cookie header that clears the
    /// refreshToken cookie: empty value, Max-Age=0 (immediate expiry).
    /// </summary>
    private static void AssertContainsClearSetCookie(HttpResponseMessage response)
    {
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var setCookieValues),
            "Expected Set-Cookie header in the response.");

        var clearHeader = setCookieValues
            .FirstOrDefault(h => h.StartsWith("refreshToken=", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(clearHeader);
        Assert.Contains("refreshToken=;", clearHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Max-Age=0", clearHeader, StringComparison.OrdinalIgnoreCase);
        // The path must be consistent so the browser replaces (not appends) the cookie
        Assert.Contains("path=/auth", clearHeader, StringComparison.OrdinalIgnoreCase);
    }

    // Inline contracts for test deserialization — avoid coupling to API DTOs.
    private sealed record TokenResponseContract(string AccessToken, int ExpiresIn);
}
