using Project.Domain.ValueObjects.Ids;

namespace Project.UnitTests.ValueObjects.Ids;

public class GuidIdTests
{
    [Fact]
    public void UserId_New_ProducesNonEmpty()
    {
        var id = UserId.New();
        Assert.NotEqual(Guid.Empty, (Guid)id);
    }

    [Fact]
    public void UserId_New_ProducesUniqueValues()
    {
        var id1 = UserId.New();
        var id2 = UserId.New();
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void UserId_From_RoundTripsGuid()
    {
        var guid = Guid.NewGuid();
        var id = UserId.From(guid);
        Assert.Equal(guid, (Guid)id);
    }

    [Fact]
    public void UserId_From_EmptyGuid_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => UserId.From(Guid.Empty));
        Assert.Contains("empty", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void UserId_ValueEquality_SameGuidEqual()
    {
        var guid = Guid.NewGuid();
        var id1 = UserId.From(guid);
        var id2 = UserId.From(guid);
        Assert.Equal(id1, id2);
    }

    [Fact]
    public void UserId_ValueEquality_DifferentGuidNotEqual()
    {
        var id1 = UserId.From(Guid.NewGuid());
        var id2 = UserId.From(Guid.NewGuid());
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void UserId_ImplicitGuidConversion_Identity()
    {
        var guid = Guid.NewGuid();
        Guid converted = UserId.From(guid);
        Assert.Equal(guid, converted);
    }

    [Fact]
    public void UserId_Default_IsNull()
    {
        UserId? @default = default;
        Assert.Null(@default);
    }

    [Fact]
    public void RoleId_FactoriesAndEquality()
    {
        var guid = Guid.NewGuid();
        var id = RoleId.From(guid);
        Assert.Equal(guid, (Guid)id);
        Assert.NotEqual(RoleId.New(), RoleId.New());
    }

    [Fact]
    public void RoleId_From_EmptyGuid_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => RoleId.From(Guid.Empty));
        Assert.Contains("empty", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void RoleId_Default_IsNull()
    {
        RoleId? @default = default;
        Assert.Null(@default);
    }

    [Fact]
    public void PermissionId_FactoriesAndEquality()
    {
        var guid = Guid.NewGuid();
        var id = PermissionId.From(guid);
        Assert.Equal(guid, (Guid)id);
        Assert.NotEqual(PermissionId.New(), PermissionId.New());
    }

    [Fact]
    public void PermissionId_From_EmptyGuid_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => PermissionId.From(Guid.Empty));
        Assert.Contains("empty", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void PermissionId_Default_IsNull()
    {
        PermissionId? @default = default;
        Assert.Null(@default);
    }

    [Fact]
    public void RefreshTokenId_FactoriesAndEquality()
    {
        var guid = Guid.NewGuid();
        var id = RefreshTokenId.From(guid);
        Assert.Equal(guid, (Guid)id);
        Assert.NotEqual(RefreshTokenId.New(), RefreshTokenId.New());
    }

    [Fact]
    public void RefreshTokenId_From_EmptyGuid_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => RefreshTokenId.From(Guid.Empty));
        Assert.Contains("empty", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void RefreshTokenId_Default_IsNull()
    {
        RefreshTokenId? @default = default;
        Assert.Null(@default);
    }

    [Fact]
    public void MenuItemId_FactoriesAndEquality()
    {
        var guid = Guid.NewGuid();
        var id = MenuItemId.From(guid);
        Assert.Equal(guid, (Guid)id);
        Assert.NotEqual(MenuItemId.New(), MenuItemId.New());
    }

    [Fact]
    public void MenuItemId_From_EmptyGuid_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => MenuItemId.From(Guid.Empty));
        Assert.Contains("empty", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void MenuItemId_Default_IsNull()
    {
        MenuItemId? @default = default;
        Assert.Null(@default);
    }
}

public class CompositeIdTests
{
    [Fact]
    public void UserRoleId_From_Constructs_WithUserIdAndRoleId()
    {
        var userId = UserId.New();
        var roleId = RoleId.New();

        var userRoleId = UserRoleId.From(userId, roleId);

        Assert.Equal(userId, userRoleId.UserId);
        Assert.Equal(roleId, userRoleId.RoleId);
    }

    [Fact]
    public void UserRoleId_ValueEquality_SameComponentsEqual()
    {
        var userId = UserId.New();
        var roleId = RoleId.New();

        var id1 = UserRoleId.From(userId, roleId);
        var id2 = UserRoleId.From(userId, roleId);

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void UserRoleId_ValueEquality_DifferentComponentsNotEqual()
    {
        var id1 = UserRoleId.From(UserId.New(), RoleId.New());
        var id2 = UserRoleId.From(UserId.New(), RoleId.New());

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void UserRoleId_From_NullUserId_Throws()
    {
        var roleId = RoleId.New();
        var ex = Assert.Throws<ArgumentNullException>(() => UserRoleId.From(null!, roleId));
        Assert.Equal("userId", ex.ParamName);
    }

    [Fact]
    public void UserRoleId_From_NullRoleId_Throws()
    {
        var userId = UserId.New();
        var ex = Assert.Throws<ArgumentNullException>(() => UserRoleId.From(userId, null!));
        Assert.Equal("roleId", ex.ParamName);
    }

    [Fact]
    public void UserRoleId_Default_IsNull()
    {
        UserRoleId? @default = default;
        Assert.Null(@default);
    }

    [Fact]
    public void RolePermissionId_From_Constructs_WithRoleIdAndPermissionId()
    {
        var roleId = RoleId.New();
        var permissionId = PermissionId.New();

        var rolePermissionId = RolePermissionId.From(roleId, permissionId);

        Assert.Equal(roleId, rolePermissionId.RoleId);
        Assert.Equal(permissionId, rolePermissionId.PermissionId);
    }

    [Fact]
    public void RolePermissionId_ValueEquality_SameComponentsEqual()
    {
        var roleId = RoleId.New();
        var permissionId = PermissionId.New();

        var id1 = RolePermissionId.From(roleId, permissionId);
        var id2 = RolePermissionId.From(roleId, permissionId);

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void RolePermissionId_ValueEquality_DifferentComponentsNotEqual()
    {
        var id1 = RolePermissionId.From(RoleId.New(), PermissionId.New());
        var id2 = RolePermissionId.From(RoleId.New(), PermissionId.New());

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void RolePermissionId_From_NullRoleId_Throws()
    {
        var permissionId = PermissionId.New();
        var ex = Assert.Throws<ArgumentNullException>(() => RolePermissionId.From(null!, permissionId));
        Assert.Equal("roleId", ex.ParamName);
    }

    [Fact]
    public void RolePermissionId_From_NullPermissionId_Throws()
    {
        var roleId = RoleId.New();
        var ex = Assert.Throws<ArgumentNullException>(() => RolePermissionId.From(roleId, null!));
        Assert.Equal("permissionId", ex.ParamName);
    }

    [Fact]
    public void RolePermissionId_Default_IsNull()
    {
        RolePermissionId? @default = default;
        Assert.Null(@default);
    }

    [Fact]
    public void UserRoleId_Deconstruct_DestructuresIntoComponents()
    {
        var userId = UserId.New();
        var roleId = RoleId.New();
        var id = UserRoleId.From(userId, roleId);

        var (u, r) = id;

        Assert.Equal(userId, u);
        Assert.Equal(roleId, r);
    }

    [Fact]
    public void RolePermissionId_Deconstruct_DestructuresIntoComponents()
    {
        var roleId = RoleId.New();
        var permissionId = PermissionId.New();
        var id = RolePermissionId.From(roleId, permissionId);

        var (r, p) = id;

        Assert.Equal(roleId, r);
        Assert.Equal(permissionId, p);
    }
}
