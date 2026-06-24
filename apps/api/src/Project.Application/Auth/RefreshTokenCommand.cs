namespace Project.Application.Auth;

/// <summary>
/// Command to refresh an access token using a valid refresh token.
/// The raw refresh token value is received from the client cookie.
/// </summary>
public sealed record RefreshTokenCommand(string RefreshTokenRaw) : ICommand;
