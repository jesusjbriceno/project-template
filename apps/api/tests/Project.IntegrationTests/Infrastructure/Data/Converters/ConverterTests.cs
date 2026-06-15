using Project.Domain.ValueObjects.Ids;
using Project.Infrastructure.Data.Converters;

namespace Project.IntegrationTests.Infrastructure.Data.Converters;

/// <summary>
/// Unit tests for strongly-typed ID converters and comparers.
/// Verifies round-trip conversion: domain → store → domain.
/// </summary>
public sealed class IdConverterTests
{
    [Fact]
    public void UserId_RoundTrip_Produces_SameValue()
    {
        var original = UserId.New();
        var converter = new UserIdConverter();

        var stored = converter.ConvertToProvider(original);
        var restored = converter.ConvertFromProvider(stored!);

        Assert.Equal(original, restored);
    }

    [Fact]
    public void RoleId_RoundTrip_Produces_SameValue()
    {
        var original = RoleId.New();
        var converter = new RoleIdConverter();

        var stored = converter.ConvertToProvider(original);
        var restored = converter.ConvertFromProvider(stored!);

        Assert.Equal(original, restored);
    }

    [Fact]
    public void PermissionId_RoundTrip_Produces_SameValue()
    {
        var original = PermissionId.New();
        var converter = new PermissionIdConverter();

        var stored = converter.ConvertToProvider(original);
        var restored = converter.ConvertFromProvider(stored!);

        Assert.Equal(original, restored);
    }

    [Fact]
    public void RefreshTokenId_RoundTrip_Produces_SameValue()
    {
        var original = RefreshTokenId.New();
        var converter = new RefreshTokenIdConverter();

        var stored = converter.ConvertToProvider(original);
        var restored = converter.ConvertFromProvider(stored!);

        Assert.Equal(original, restored);
    }

    [Fact]
    public void MenuItemId_RoundTrip_Produces_SameValue()
    {
        var original = MenuItemId.New();
        var converter = new MenuItemIdConverter();

        var stored = converter.ConvertToProvider(original);
        var restored = converter.ConvertFromProvider(stored!);

        Assert.Equal(original, restored);
    }

    [Fact]
    public void UserRoleId_RoundTrip_Produces_SameValue()
    {
        var userId = UserId.New();
        var roleId = RoleId.New();
        var original = UserRoleId.From(userId, roleId);
        var converter = new UserRoleIdConverter();

        var stored = converter.ConvertToProvider(original);
        var restored = converter.ConvertFromProvider(stored!);

        Assert.Equal(original, restored);
    }

    [Fact]
    public void RolePermissionId_RoundTrip_Produces_SameValue()
    {
        var roleId = RoleId.New();
        var permissionId = PermissionId.New();
        var original = RolePermissionId.From(roleId, permissionId);
        var converter = new RolePermissionIdConverter();

        var stored = converter.ConvertToProvider(original);
        var restored = converter.ConvertFromProvider(stored!);

        Assert.Equal(original, restored);
    }

    [Fact]
    public void UserId_Comparer_DetectsSameGuidAsEqual()
    {
        var guid = Guid.NewGuid();
        var id1 = UserId.From(guid);
        var id2 = UserId.From(guid);

        var comparer = new UserIdComparer();
        Assert.True(comparer.Equals(id1, id2));
    }

    [Fact]
    public void UserId_Comparer_DetectsDifferentGuidAsNotEqual()
    {
        var id1 = UserId.New();
        var id2 = UserId.New();

        var comparer = new UserIdComparer();
        Assert.False(comparer.Equals(id1, id2));
    }
}
