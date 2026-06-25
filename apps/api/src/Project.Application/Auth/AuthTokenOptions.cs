namespace Project.Application.Auth;

/// <summary>
/// Application-owned token lifetime configuration.
/// Populated by Infrastructure from JwtOptions at composition root.
/// Keeps the Application layer free of Infrastructure configuration types.
/// </summary>
public sealed class AuthTokenOptions
{
    /// <summary>
    /// Refresh token lifetime in days. Defaults to 7.
    /// Must be between 1 and 365 (validated at the Infrastructure boundary).
    /// </summary>
    public int RefreshTokenDays { get; init; } = 7;
}
