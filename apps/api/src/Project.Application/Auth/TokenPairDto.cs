namespace Project.Application.Auth;

/// <summary>
/// Result of a successful login or refresh operation.
/// Contains the signed JWT access token, the time until it expires,
/// and the raw refresh token string for the API layer to set in the HttpOnly cookie.
/// </summary>
public sealed record TokenPairDto(string AccessToken, int ExpiresInSeconds, string RefreshToken);
