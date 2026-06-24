using FluentValidation;
using FluentValidation.TestHelper;
using Project.Application.Auth;

namespace Project.ApplicationTests.Auth;

public sealed class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        // ARRANGE
        var command = new LoginCommand("user@example.com", "StrongP@ss1!");

        // ACT
        var result = _validator.TestValidate(command);

        // ASSERT
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespaceEmail_ShouldHaveValidationError(string email)
    {
        // ARRANGE
        var command = new LoginCommand(email, "StrongP@ss1!");

        // ACT
        var result = _validator.TestValidate(command);

        // ASSERT
        result.ShouldHaveValidationErrorFor(c => c.Email)
              .WithErrorCode("NotEmptyValidator");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    [InlineData("@nouser.com")]
    public void Validate_InvalidEmailFormat_ShouldHaveValidationError(string email)
    {
        // ARRANGE
        var command = new LoginCommand(email, "StrongP@ss1!");

        // ACT
        var result = _validator.TestValidate(command);

        // ASSERT
        result.ShouldHaveValidationErrorFor(c => c.Email)
              .WithErrorCode("EmailValidator");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespacePassword_ShouldHaveValidationError(string password)
    {
        // ARRANGE
        var command = new LoginCommand("user@example.com", password);

        // ACT
        var result = _validator.TestValidate(command);

        // ASSERT
        result.ShouldHaveValidationErrorFor(c => c.Password)
              .WithErrorCode("NotEmptyValidator");
    }
}
