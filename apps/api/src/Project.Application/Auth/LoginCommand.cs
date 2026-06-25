namespace Project.Application.Auth;

/// <summary>
/// Command to authenticate a user with email and password.
/// Returns <see cref="Common.Result{TokenPairDto}"/> on success.
/// </summary>
public sealed record LoginCommand(string Email, string Password) : ICommand;
