using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Project.IntegrationTests.Auth;

/// <summary>
/// Test-only protected endpoint used by <see cref="AuthMiddlewareTests"/>
/// to verify that the JWT Bearer middleware returns 401 for unauthenticated
/// or invalid requests.
///
/// This controller lives ONLY in the integration test project and is
/// registered via <see cref="AuthWebApplicationFactory"/>. It does NOT
/// appear in production API surface.
/// </summary>
[ApiController]
[Route("test-auth")]
public sealed class TestAuthController : ControllerBase
{
    /// <summary>
    /// Returns 200 if a valid JWT access token is provided (via [Authorize]).
    /// Returns 401 if no token or invalid token.
    /// </summary>
    [HttpGet("protected")]
    [Authorize]
    public IActionResult Get() => Ok(new { message = "authenticated" });
}
