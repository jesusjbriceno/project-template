using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Project.Api.Controllers.Contracts;
using Project.Api.Controllers.Middleware;
using Project.Application.Auth;
using Project.Application.Common;
using Project.Infrastructure.Security;

namespace Project.Api.Controllers.Controllers;

/// <summary>
/// Authentication endpoints: login, refresh, logout.
///
/// All auth responses use a generic structure so the caller cannot
/// enumerate users based on error messages.
/// </summary>
[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private const string SafeRefreshFailureDetail = "Refresh token is invalid or no longer usable.";

    private readonly LoginCommandHandler _loginHandler;
    private readonly RefreshTokenCommandHandler _refreshHandler;
    private readonly LogoutCommandHandler _logoutHandler;
    private readonly IValidator<LoginCommand> _loginValidator;
    private readonly IValidator<RefreshTokenCommand> _refreshValidator;
    private readonly IValidator<LogoutCommand> _logoutValidator;
    private readonly JwtOptions _jwtOptions;
    private readonly IWebHostEnvironment _env;

    public AuthController(
        LoginCommandHandler loginHandler,
        RefreshTokenCommandHandler refreshHandler,
        LogoutCommandHandler logoutHandler,
        IValidator<LoginCommand> loginValidator,
        IValidator<RefreshTokenCommand> refreshValidator,
        IValidator<LogoutCommand> logoutValidator,
        IOptions<JwtOptions> jwtOptions,
        IWebHostEnvironment env)
    {
        _loginHandler = loginHandler ?? throw new ArgumentNullException(nameof(loginHandler));
        _refreshHandler = refreshHandler ?? throw new ArgumentNullException(nameof(refreshHandler));
        _logoutHandler = logoutHandler ?? throw new ArgumentNullException(nameof(logoutHandler));
        _loginValidator = loginValidator ?? throw new ArgumentNullException(nameof(loginValidator));
        _refreshValidator = refreshValidator ?? throw new ArgumentNullException(nameof(refreshValidator));
        _logoutValidator = logoutValidator ?? throw new ArgumentNullException(nameof(logoutValidator));
        _jwtOptions = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
        _env = env ?? throw new ArgumentNullException(nameof(env));
    }

    // ── Login ─────────────────────────────────────────────────

    /// <summary>
    /// Authenticates a user with email and password.
    /// On success: returns the access token in the response body and sets
    /// the refresh token as an HttpOnly, Secure, SameSite=Strict cookie.
    /// On failure: returns 401 with a generic message.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // 1. Map to command
        var command = (LoginCommand)request;

        // 2. Validate
        var validationResult = await _loginValidator.ValidateAsync(command, HttpContext.RequestAborted);
        if (!validationResult.IsValid)
            return ResultProblemDetailsMapper.Map(
                Result.Failure(ErrorCodes.Auth.InvalidCredentials, "Invalid credentials."));

        // 3. Handle
        var result = await _loginHandler.Handle(command, HttpContext.RequestAborted);

        // 4. Map result → HTTP
        if (result.IsFailure)
            return ResultProblemDetailsMapper.Map(result);

        // 5. Set refresh token cookie
        SetRefreshTokenCookie(result.Value!.RefreshToken);

        // 6. Return access token in body
        var response = (TokenResponse)result.Value;
        return Ok(response);
    }

    // ── Refresh ───────────────────────────────────────────────

    /// <summary>
    /// Issues a new access token and rotated refresh token.
    /// Requires a valid refresh token cookie.
    /// On reuse detection: revokes the entire token family.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh()
    {
        // 1. Read cookie
        var refreshTokenRaw = Request.Cookies["refreshToken"];
        if (string.IsNullOrWhiteSpace(refreshTokenRaw))
            return ResultProblemDetailsMapper.Map(
                Result.Failure(ErrorCodes.Auth.RefreshTokenMissing, "Refresh token is required."));

        // 2. Create and validate command
        var command = new RefreshTokenCommand(refreshTokenRaw);
        var validationResult = await _refreshValidator.ValidateAsync(command, HttpContext.RequestAborted);
        if (!validationResult.IsValid)
            return ResultProblemDetailsMapper.Map(
                Result.Failure(ErrorCodes.Auth.RefreshTokenMissing, "Refresh token is required."));

        // 3. Handle
        var result = await _refreshHandler.Handle(command, HttpContext.RequestAborted);

        // 4. Map result → HTTP
        if (result.IsFailure)
        {
            // Clear cookie on token expiry, revocation, or reuse
            ClearRefreshTokenCookie();
            return ResultProblemDetailsMapper.Map(
                Result.Failure(result.Error.Code, SafeRefreshFailureDetail));
        }

        // 5. Set rotated refresh token cookie
        SetRefreshTokenCookie(result.Value!.RefreshToken);

        // 6. Return new access token in body
        var response = (TokenResponse)result.Value;
        return Ok(response);
    }

    // ── Logout ────────────────────────────────────────────────

    /// <summary>
    /// Revokes the entire token family and clears the refresh token cookie.
    /// Idempotent — already-revoked or missing tokens return success.
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout()
    {
        // 1. Read cookie
        var refreshTokenRaw = Request.Cookies["refreshToken"];
        if (string.IsNullOrWhiteSpace(refreshTokenRaw))
            return ResultProblemDetailsMapper.Map(
                Result.Failure(ErrorCodes.Auth.RefreshTokenMissing, "Refresh token is required."));

        // 2. Create and validate command
        var command = new LogoutCommand(refreshTokenRaw);
        var validationResult = await _logoutValidator.ValidateAsync(command, HttpContext.RequestAborted);
        if (!validationResult.IsValid)
            return ResultProblemDetailsMapper.Map(
                Result.Failure(ErrorCodes.Auth.RefreshTokenMissing, "Refresh token is required."));

        // 3. Handle (idempotent)
        await _logoutHandler.Handle(command, HttpContext.RequestAborted);

        // 4. Clear cookie
        ClearRefreshTokenCookie();

        // 5. Return success
        return NoContent();
    }

    // ── Cookie helpers ────────────────────────────────────────

    /// <summary>
    /// Sets the refresh token as an HttpOnly, Secure, SameSite=Strict cookie.
    /// Secure policy: <c>SameAsRequest</c> in Development, <c>Always</c> otherwise.
    /// </summary>
    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(), // Always in prod; SameAsRequest in dev (cookie not flagged Secure)
            SameSite = SameSiteMode.Strict,
            Path = "/auth",
            MaxAge = TimeSpan.FromDays(_jwtOptions.RefreshTokenDays),
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    /// <summary>
    /// Expires the refresh token cookie by setting Max-Age to zero.
    /// Must use the same Path, Domain, and SameSite as <see cref="SetRefreshTokenCookie"/>
    /// so the browser correctly replaces the cookie instead of creating a new scoped one.
    /// </summary>
    private void ClearRefreshTokenCookie()
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(), // Always in prod; SameAsRequest in dev
            SameSite = SameSiteMode.Strict,
            Path = "/auth",
            MaxAge = TimeSpan.Zero, // expire immediately
        };
        Response.Cookies.Append("refreshToken", string.Empty, cookieOptions);
    }
}
