namespace Project.Application.Auth;

/// <summary>
/// Command to revoke a refresh token (logout).
/// The raw refresh token value is received from the client cookie.
/// </summary>
public sealed record LogoutCommand(string RefreshTokenRaw) : ICommand;
