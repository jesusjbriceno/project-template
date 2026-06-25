using FluentValidation;

namespace Project.Application.Auth;

/// <summary>
/// Validates <see cref="RefreshTokenCommand"/> inputs.
/// Enforces non-empty, non-whitespace refresh token value.
/// </summary>
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshTokenRaw)
            .NotEmpty();
    }
}
