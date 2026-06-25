using FluentValidation;

namespace Project.Application.Auth;

/// <summary>
/// Validates <see cref="LoginCommand"/> inputs before the handler executes.
/// Enforces: non-empty email with valid format, non-empty password.
/// </summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
