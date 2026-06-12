using Project.Domain.Errors;
using Project.Domain.ValueObjects;

namespace Project.UnitTests.ValueObjects;

public class EmailTests
{
    [Fact]
    public void Create_ValidEmail_ReturnsEmailWithNormalizedValue()
    {
        var email = Email.Create("User@Example.com");

        Assert.Equal("user@example.com", email.Value);
    }

    [Fact]
    public void Create_AlreadyNormalizedEmail_ReturnsSameValue()
    {
        var email = Email.Create("user@example.com");

        Assert.Equal("user@example.com", email.Value);
    }

    [Fact]
    public void Create_EmailWithSurroundingWhitespace_TrimsWhitespace()
    {
        var email = Email.Create("  user@example.com  ");

        Assert.Equal("user@example.com", email.Value);
    }

    [Fact]
    public void Create_NullValue_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Email.Create(null!));
    }

    [Fact]
    public void Create_EmptyString_ThrowsInvalidEmailException()
    {
        var ex = Assert.Throws<InvalidEmailException>(() => Email.Create(""));
        Assert.Contains("valid email address", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void Create_WhitespaceOnly_ThrowsInvalidEmailException()
    {
        var ex = Assert.Throws<InvalidEmailException>(() => Email.Create("   "));
        Assert.Contains("valid email address", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void Create_MissingAtSign_ThrowsInvalidEmailException()
    {
        var ex = Assert.Throws<InvalidEmailException>(() => Email.Create("userexample.com"));
        Assert.Contains("valid email address", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void Create_MultipleAtSigns_ThrowsInvalidEmailException()
    {
        var ex = Assert.Throws<InvalidEmailException>(() => Email.Create("user@@example.com"));
        Assert.Contains("valid email address", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void Create_ValidEmailWithMixedCase_ThrowsWithContext()
    {
        // The exception message should include the invalid value for debugging
        var ex = Assert.Throws<InvalidEmailException>(() => Email.Create("not-an-email"));
        Assert.Contains("not-an-email", ex.Message);
    }

    [Fact]
    public void Create_AtSignOnly_NoLocalPart_ThrowsInvalidEmailException()
    {
        var ex = Assert.Throws<InvalidEmailException>(() => Email.Create("@example.com"));
        Assert.Contains("valid email address", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void Create_AtSignOnly_NoDomainPart_ThrowsInvalidEmailException()
    {
        var ex = Assert.Throws<InvalidEmailException>(() => Email.Create("user@"));
        Assert.Contains("valid email address", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void Create_SpaceInLocalPart_ThrowsInvalidEmailException()
    {
        var ex = Assert.Throws<InvalidEmailException>(() => Email.Create("user example@example.com"));
        Assert.Contains("valid email address", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void Create_NoDotInDomainPart_ThrowsInvalidEmailException()
    {
        var ex = Assert.Throws<InvalidEmailException>(() => Email.Create("user@example"));
        Assert.Contains("valid email address", ex.Message.ToLowerInvariant());
    }

    [Theory]
    [InlineData("user@example.com", "user@example.com")]
    [InlineData("User@Example.com", "user@example.com")]
    [InlineData("USER@EXAMPLE.COM", "user@example.com")]
    [InlineData("user+alias@example.com", "user+alias@example.com")]
    [InlineData("user.name@sub.example.co.uk", "user.name@sub.example.co.uk")]
    public void Create_ValidEmails_NormalizesCorrectly(string input, string expected)
    {
        var email = Email.Create(input);

        Assert.Equal(expected, email.Value);
    }

    [Fact]
    public void Equals_SameNormalizedValue_ReturnsTrue()
    {
        var email1 = Email.Create("User@Example.com");
        var email2 = Email.Create("user@example.com");

        Assert.True(email1.Equals(email2));
        Assert.True(email1 == email2);
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var email1 = Email.Create("user@example.com");
        var email2 = Email.Create("other@example.com");

        Assert.False(email1.Equals(email2));
        Assert.False(email1 == email2);
        Assert.True(email1 != email2);
    }

    [Fact]
    public void GetHashCode_SameNormalizedValue_ReturnsSameHashCode()
    {
        var email1 = Email.Create("User@Example.com");
        var email2 = Email.Create("user@example.com");

        Assert.Equal(email1.GetHashCode(), email2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsNormalizedValue()
    {
        var email = Email.Create("User@Example.com");

        Assert.Equal("user@example.com", email.ToString());
    }
}
