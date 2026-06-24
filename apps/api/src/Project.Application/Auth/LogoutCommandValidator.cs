using FluentValidation;

namespace Project.Application.Auth;

/// <summary>
/// Validates <see cref="LogoutCommand"/> inputs.
/// Enforces non-empty, non-whitespace refresh token value.
/// </summary>
public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshTokenRaw)
            .NotEmpty();
    }
}
