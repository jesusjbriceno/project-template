using Project.Application.Abstractions.Security;
using Project.Infrastructure.Security;

namespace Project.UnitTests.Security;

public class BCryptPasswordHasherTests
{
    private readonly IPasswordHasher _hasher = new BCryptPasswordHasher();

    [Fact]
    public void Hash_ReturnsNonNullString()
    {
        var hash = _hasher.Hash("a-valid-password-123");

        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void Hash_ProducesDifferentOutputForDifferentInputs()
    {
        var hash1 = _hasher.Hash("password-A");
        var hash2 = _hasher.Hash("password-B");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        const string plaintext = "MySecretP@ssw0rd!";
        var hash = _hasher.Hash(plaintext);

        var result = _hasher.Verify(plaintext, hash);

        Assert.True(result);
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("correct-password");

        var result = _hasher.Verify("wrong-password", hash);

        Assert.False(result);
    }

    [Fact]
    public void Verify_CaseSensitive_ReturnsFalseWhenCaseDiffers()
    {
        var hash = _hasher.Hash("CaseSensitive");

        var result = _hasher.Verify("casesensitive", hash);

        Assert.False(result);
    }

    [Fact]
    public void Hash_WithNullInput_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => _hasher.Hash(null!));
        Assert.Contains("plaintext", ex.ParamName!);
    }

    [Fact]
    public void Verify_WithNullPlaintext_ThrowsArgumentNullException()
    {
        var hash = _hasher.Hash("some-password");

        var ex = Assert.Throws<ArgumentNullException>(() => _hasher.Verify(null!, hash));
        Assert.Contains("plaintext", ex.ParamName!);
    }

    [Fact]
    public void Verify_WithNullHash_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => _hasher.Verify("password", null!));
        Assert.Contains("hash", ex.ParamName!);
    }

    [Fact]
    public void Verify_WithMalformedHash_ReturnsFalse()
    {
        var result = _hasher.Verify("password", "not-a-valid-bcrypt-hash");

        Assert.False(result);
    }

    [Fact]
    public void Hash_SameInputTwice_ProducesDifferentHashes()
    {
        var hash1 = _hasher.Hash("same-password");
        var hash2 = _hasher.Hash("same-password");

        Assert.NotEqual(hash1, hash2);
    }
}
