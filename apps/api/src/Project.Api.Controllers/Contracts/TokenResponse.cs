using Project.Application.Auth;

namespace Project.Api.Controllers.Contracts;

/// <summary>
/// Response body for successful auth operations (login and refresh).
/// Contains the JWT access token and its remaining lifetime in seconds.
/// Mapped from <see cref="TokenPairDto"/> via explicit operator.
/// </summary>
public sealed record TokenResponse(string AccessToken, int ExpiresIn)
{
    /// <summary>
    /// Converts a <see cref="TokenPairDto"/> to a <see cref="TokenResponse"/>.
    /// </summary>
    public static explicit operator TokenResponse(TokenPairDto dto) =>
        new(dto.AccessToken, dto.ExpiresInSeconds);
}
