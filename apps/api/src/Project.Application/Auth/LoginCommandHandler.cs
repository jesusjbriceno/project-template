using Project.Application.Abstractions.Messaging;
using Project.Domain.Common;
using Project.Domain.Entities;
using Project.Domain.ValueObjects;
using Project.Application.Common;

namespace Project.Application.Auth;

/// <summary>
/// Handles login authentication: validates credentials, checks user status,
/// enforces password policy, and issues access + refresh tokens.
/// Returns the same generic 401 for all authentication failures
/// (user not found, wrong password, blocked, weak password).
/// </summary>
public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, TokenPairDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IClock _clock;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenRepository refreshTokenRepository,
        IClock clock,
        ITokenService tokenService)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }

    /// <summary>
    /// Authenticates a user and returns an access token DTO on success.
    /// All failure paths return generic 401-class errors to prevent enumeration.
    /// </summary>
    public async Task<Result<TokenPairDto>> Handle(LoginCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        // ── Password policy check (before DB hit, early exit) ──
        if (!PasswordPolicy.IsStrongEnough(command.Password))
            return Result<TokenPairDto>.Failure(
                ErrorCodes.Auth.InvalidCredentials,
                "Invalid credentials.");

        // ── Look up user with roles ──
        var email = Email.Create(command.Email);
        var (user, roles) = await _userRepository.GetByEmailWithRolesAsync(email, ct);

        if (user is null)
            return Result<TokenPairDto>.Failure(
                ErrorCodes.Auth.InvalidCredentials,
                "Invalid credentials.");

        // ── Blocked check — must not leak account state ──
        // Return the same generic InvalidCredentials as all other failures.
        if (user.IsBlocked)
            return Result<TokenPairDto>.Failure(
                ErrorCodes.Auth.InvalidCredentials,
                "Invalid credentials.");

        // ── Password verification ──
        if (!_passwordHasher.Verify(command.Password, user.PasswordHash))
            return Result<TokenPairDto>.Failure(
                ErrorCodes.Auth.InvalidCredentials,
                "Invalid credentials.");

        // ── Issue tokens ──
        var (accessToken, lifetime) = _jwtTokenService.GenerateAccessToken(user, roles);
        var (rawRefresh, refreshHash) = _jwtTokenService.GenerateRefreshToken();

        var refreshExpiry = _clock.UtcNow.AddDays(7); // matches default JwtOptions.RefreshTokenDays
        var refreshToken = RefreshToken.Create(
            user.Id,
            refreshHash,
            Guid.NewGuid(), // new family per login
            refreshExpiry,
            _clock);

        await _refreshTokenRepository.AddAsync(refreshToken, ct);

        return Result<TokenPairDto>.Success(new TokenPairDto(
            accessToken,
            (int)lifetime.TotalSeconds,
            rawRefresh));
    }
}
