using Project.Domain.Policies;

namespace Project.UnitTests.Policies;

public class DeletionPolicyTests
{
    [Fact]
    public void Constructor_SetsAllDimensions()
    {
        var policy = new DeletionPolicy(
            SoftDeleteEnabled: true,
            RecycleBinVisible: false,
            RestoreAllowed: false,
            HardDeleteAllowed: false);

        Assert.True(policy.SoftDeleteEnabled);
        Assert.False(policy.RecycleBinVisible);
        Assert.False(policy.RestoreAllowed);
        Assert.False(policy.HardDeleteAllowed);
    }

    [Fact]
    public void Dimensions_AreIndependent()
    {
        // All combinations of dimensions should be possible
        var policy1 = new DeletionPolicy(true, true, true, true);
        var policy2 = new DeletionPolicy(true, false, false, false);
        var policy3 = new DeletionPolicy(false, false, false, true);
        var policy4 = new DeletionPolicy(true, true, false, false);
        var policy5 = new DeletionPolicy(false, false, false, false);

        Assert.True(policy1.SoftDeleteEnabled && policy1.RecycleBinVisible && policy1.RestoreAllowed && policy1.HardDeleteAllowed);
        Assert.True(policy2.SoftDeleteEnabled && !policy2.RecycleBinVisible && !policy2.RestoreAllowed && !policy2.HardDeleteAllowed);
        Assert.True(!policy3.SoftDeleteEnabled && !policy3.RecycleBinVisible && !policy3.RestoreAllowed && policy3.HardDeleteAllowed);
        Assert.True(policy4.SoftDeleteEnabled && policy4.RecycleBinVisible && !policy4.RestoreAllowed && !policy4.HardDeleteAllowed);
    }

    [Fact]
    public void UserDefaultPolicy_SoftDeleteOnly()
    {
        var policy = DeletionPolicy.UserDefault;

        Assert.True(policy.SoftDeleteEnabled);
        Assert.False(policy.RecycleBinVisible);
        Assert.False(policy.RestoreAllowed);
        Assert.False(policy.HardDeleteAllowed);
    }

    [Fact]
    public void RefreshTokenDefaultPolicy_NoRecycleBinSemantics()
    {
        var policy = DeletionPolicy.RefreshTokenDefault;

        // RefreshToken has no recycle-bin semantics;
        // lifecycle is governed by revocation/expiration
        Assert.False(policy.RecycleBinVisible);
    }

    [Fact]
    public void RoleDefaultPolicy_AllowsSoftDeleteAndRestore()
    {
        var policy = DeletionPolicy.RoleDefault;

        // Roles can be soft-deleted and restored (subject to consistency rules)
        Assert.True(policy.SoftDeleteEnabled);
        Assert.True(policy.RestoreAllowed);
    }

    [Fact]
    public void MenuItemDefaultPolicy_AllowsSoftDeleteAndRestore()
    {
        var policy = DeletionPolicy.MenuItemDefault;

        // MenuItems can be soft-deleted and restored (subject to hierarchy consistency)
        Assert.True(policy.SoftDeleteEnabled);
        Assert.True(policy.RestoreAllowed);
    }

    [Fact]
    public void PermissionDefaultPolicy_HardDeleteOnly()
    {
        var policy = DeletionPolicy.PermissionDefault;

        // Permissions are catalog entries; no soft delete needed
        Assert.False(policy.SoftDeleteEnabled);
        Assert.False(policy.RecycleBinVisible);
        Assert.False(policy.RestoreAllowed);
        Assert.True(policy.HardDeleteAllowed);
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var p1 = new DeletionPolicy(true, false, false, false);
        var p2 = new DeletionPolicy(true, false, false, false);

        Assert.True(p1 == p2);
        Assert.Equal(p1, p2);
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var p1 = new DeletionPolicy(true, false, false, false);
        var p2 = new DeletionPolicy(false, false, false, false);

        Assert.False(p1 == p2);
        Assert.NotEqual(p1, p2);
    }

    [Fact]
    public void WithExpression_PreservesOtherDimensions()
    {
        var original = DeletionPolicy.UserDefault;
        var modified = original with { HardDeleteAllowed = true };

        Assert.True(modified.SoftDeleteEnabled);
        Assert.False(modified.RecycleBinVisible);
        Assert.False(modified.RestoreAllowed);
        Assert.True(modified.HardDeleteAllowed);
    }
}
