using Project.Application.Common;
using Project.MigrationService;

namespace Project.UnitTests.MigrationService;

public class SuperadminCredentialValidatorTests
{
    private const string ValidEmail = "admin@project.local";
    private const string ValidPassword = "StrongP@ssw0rd123!"; // 18 chars, well above minimum

    // ── Happy path ──

    [Fact]
    public void Validate_WithValidCredentials_ReturnsSuccess()
    {
        var result = SuperadminCredentialValidator.Validate(ValidEmail, ValidPassword);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(ValidEmail.ToLowerInvariant(), result.Value.Email);
        Assert.Equal(ValidPassword, result.Value.PlaintextPassword);
    }

    // ── Missing email ──

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingEmail_ReturnsFailure(string? email)
    {
        var result = SuperadminCredentialValidator.Validate(email, ValidPassword);

        Assert.True(result.IsFailure);
        Assert.Equal("SUPERADMIN_EMAIL_MISSING", result.Error.Code);
    }

    // ── Missing password ──

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingPassword_ReturnsFailure(string? password)
    {
        var result = SuperadminCredentialValidator.Validate(ValidEmail, password);

        Assert.True(result.IsFailure);
        Assert.Equal("SUPERADMIN_PASSWORD_MISSING", result.Error.Code);
    }

    // ── Invalid email format ──

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@tld")]
    [InlineData("@no-local.com")]
    [InlineData("no-domain@")]
    [InlineData("double@@sign.com")]
    public void Validate_WithInvalidEmailFormat_ReturnsFailure(string email)
    {
        var result = SuperadminCredentialValidator.Validate(email, ValidPassword);

        Assert.True(result.IsFailure);
        Assert.Equal("SUPERADMIN_EMAIL_INVALID", result.Error.Code);
    }

    // ── Password too short ──

    [Theory]
    [InlineData("a")]           // 1 char
    [InlineData("Short1!")]     // 7 chars
    [InlineData("Abcdef12345")] // 11 chars (exactly one below minimum)
    public void Validate_WithPasswordTooShort_ReturnsFailure(string password)
    {
        var result = SuperadminCredentialValidator.Validate(ValidEmail, password);

        Assert.True(result.IsFailure);
        Assert.Equal("SUPERADMIN_PASSWORD_TOO_SHORT", result.Error.Code);
    }

    // ── Email normalization ──

    [Fact]
    public void Validate_NormalizesEmailToLowercase()
    {
        var result = SuperadminCredentialValidator.Validate("Admin@Project.Local", ValidPassword);

        Assert.True(result.IsSuccess);
        Assert.Equal("admin@project.local", result.Value!.Email);
    }

    [Theory]
    [InlineData("admin @project.local")]
    [InlineData("admin@project .local")]
    [InlineData("admin@project. local")]
    public void Validate_WithInternalWhitespaceInEmail_ReturnsFailure(string email)
    {
        var result = SuperadminCredentialValidator.Validate(email, ValidPassword);

        Assert.True(result.IsFailure);
        Assert.Equal("SUPERADMIN_EMAIL_INVALID", result.Error.Code);
    }
}
