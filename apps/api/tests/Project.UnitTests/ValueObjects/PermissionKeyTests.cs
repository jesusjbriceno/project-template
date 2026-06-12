using Project.Domain.Errors;
using Project.Domain.ValueObjects;

namespace Project.UnitTests.ValueObjects;

public class PermissionKeyTests
{
    [Fact]
    public void Create_ValidPermissionKey_ReturnsPermissionKey()
    {
        var key = PermissionKey.Create("users.create");

        Assert.Equal("users.create", key.Value);
    }

    [Fact]
    public void Create_SingleSegment_ThrowsInvalidPermissionKeyException()
    {
        var ex = Assert.Throws<InvalidPermissionKeyException>(() => PermissionKey.Create("dashboard"));
        Assert.Contains("valid permission key", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void Create_MoreThanTwoSegments_ThrowsInvalidPermissionKeyException()
    {
        var ex = Assert.Throws<InvalidPermissionKeyException>(() => PermissionKey.Create("admin.users.create"));
        Assert.Contains("valid permission key", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void Create_NullValue_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => PermissionKey.Create(null!));
    }

    [Fact]
    public void Create_EmptyString_ThrowsInvalidPermissionKeyException()
    {
        var ex = Assert.Throws<InvalidPermissionKeyException>(() => PermissionKey.Create(""));
        Assert.Contains("valid permission key", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void Create_WhitespaceOnly_ThrowsInvalidPermissionKeyException()
    {
        var ex = Assert.Throws<InvalidPermissionKeyException>(() => PermissionKey.Create("   "));
        Assert.Contains("valid permission key", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void Create_ContainsUppercase_ThrowsInvalidPermissionKeyException()
    {
        var ex = Assert.Throws<InvalidPermissionKeyException>(() => PermissionKey.Create("Users.Create"));
        Assert.Contains("valid permission key", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void Create_ContainsSpaces_ThrowsInvalidPermissionKeyException()
    {
        var ex = Assert.Throws<InvalidPermissionKeyException>(() => PermissionKey.Create("users create"));
        Assert.Contains("valid permission key", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void Create_StartsWithDot_ThrowsInvalidPermissionKeyException()
    {
        var ex = Assert.Throws<InvalidPermissionKeyException>(() => PermissionKey.Create(".users.create"));
        Assert.Contains("valid permission key", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void Create_EndsWithDot_ThrowsInvalidPermissionKeyException()
    {
        var ex = Assert.Throws<InvalidPermissionKeyException>(() => PermissionKey.Create("users.create."));
        Assert.Contains("valid permission key", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void Create_ContainsConsecutiveDots_ThrowsInvalidPermissionKeyException()
    {
        var ex = Assert.Throws<InvalidPermissionKeyException>(() => PermissionKey.Create("users..create"));
        Assert.Contains("valid permission key", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void Create_RejectsWithContextInMessage()
    {
        var ex = Assert.Throws<InvalidPermissionKeyException>(() => PermissionKey.Create("Bad Key!"));
        Assert.Contains("Bad Key!", ex.Message);
    }

    [Theory]
    [InlineData("users.read")]
    [InlineData("users.create")]
    [InlineData("users.update")]
    [InlineData("users.delete")]
    [InlineData("reports.export")]
    [InlineData("settings.view")]
    public void Create_ValidKeys_Succeeds(string input)
    {
        var key = PermissionKey.Create(input);

        Assert.Equal(input, key.Value);
    }

    [Theory]
    [InlineData("Users.Read")]
    [InlineData("USERS.CREATE")]
    [InlineData("users create")]
    [InlineData("users\tcreate")]
    [InlineData("users@create")]
    [InlineData("users#create")]
    [InlineData("users!create")]
    [InlineData("dashboard")]
    [InlineData("admin.users.manage")]
    public void Create_InvalidKeys_ThrowsInvalidPermissionKeyException(string input)
    {
        Assert.Throws<InvalidPermissionKeyException>(() => PermissionKey.Create(input));
    }

    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        var key1 = PermissionKey.Create("users.create");
        var key2 = PermissionKey.Create("users.create");

        Assert.True(key1.Equals(key2));
        Assert.True(key1 == key2);
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var key1 = PermissionKey.Create("users.create");
        var key2 = PermissionKey.Create("users.read");

        Assert.False(key1.Equals(key2));
        Assert.False(key1 == key2);
        Assert.True(key1 != key2);
    }

    [Fact]
    public void GetHashCode_SameValue_ReturnsSameHashCode()
    {
        var key1 = PermissionKey.Create("users.create");
        var key2 = PermissionKey.Create("users.create");

        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var key = PermissionKey.Create("users.create");

        Assert.Equal("users.create", key.ToString());
    }
}
