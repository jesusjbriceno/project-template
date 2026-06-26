using Microsoft.AspNetCore.Mvc;

namespace Project.IntegrationTests.Auth;

/// <summary>
/// Test-only controller that deliberately throws exceptions so integration
/// tests can verify the global <c>IExceptionHandler</c> behavior without
/// depending on production endpoints to fail.
///
/// Lives ONLY in the integration test project. Registered via
/// <see cref="AuthWebApplicationFactory"/>. Does NOT appear in the
/// production API surface.
/// </summary>
[ApiController]
[Route("test-throw")]
public sealed class ThrowController : ControllerBase
{
    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> so the test can
    /// assert the exception handler returns a safe 500 ProblemDetails.
    /// </summary>
    [HttpGet("unhandled")]
    public IActionResult Get() => throw new InvalidOperationException("Test exception — should be caught by IExceptionHandler.");
}
