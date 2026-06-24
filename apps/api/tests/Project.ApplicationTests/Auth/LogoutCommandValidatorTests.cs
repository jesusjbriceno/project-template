using FluentValidation;
using FluentValidation.TestHelper;
using Project.Application.Auth;

namespace Project.ApplicationTests.Auth;

public sealed class LogoutCommandValidatorTests
{
    private readonly LogoutCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        var command = new LogoutCommand("valid-raw-refresh-token");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespace_ShouldHaveValidationError(string rawToken)
    {
        var command = new LogoutCommand(rawToken);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.RefreshTokenRaw)
              .WithErrorCode("NotEmptyValidator");
    }
}
