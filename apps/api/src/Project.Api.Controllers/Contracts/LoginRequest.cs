using Project.Application.Auth;

namespace Project.Api.Controllers.Contracts;

/// <summary>
/// Request body for POST /auth/login.
/// Mapped to <see cref="LoginCommand"/> via explicit operator.
/// </summary>
public sealed record LoginRequest(string Email, string Password)
{
    /// <summary>
    /// Converts a <see cref="LoginRequest"/> to a <see cref="LoginCommand"/>.
    /// </summary>
    public static explicit operator LoginCommand(LoginRequest request) =>
        new(request.Email, request.Password);
}
