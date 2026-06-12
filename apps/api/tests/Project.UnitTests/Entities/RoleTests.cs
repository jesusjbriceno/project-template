using Project.Domain.Entities;
using Project.Domain.ValueObjects;
using Project.Domain.ValueObjects.Ids;
using Project.Domain.Common;
using Project.Domain.Policies;
using Project.Domain.Errors;

namespace Project.UnitTests.Entities;

public class RoleTests
{
    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow { get; init; } = new(2026, 6, 12, 10, 0, 0, TimeSpan.Zero);
    }

    [Fact]
    public void Create_WithValidName_AssignsIdAndName()
    {
        var clock = new FakeClock();

        var role = Role.Create("Administrator", false, "system", clock);

        Assert.NotEqual(default, role.Id);
        Assert.Equal("Administrator", role.Name);
        Assert.False(role.IsSystem);
        Assert.Empty(role.RolePermissions);
        Assert.Equal(clock.UtcNow, role.CreatedAt);
        Assert.Equal("system", role.CreatedBy);
    }

    [Fact]
    public void Create_AsSystemRole_SetsIsSystemTrue()
    {
        var clock = new FakeClock();

        var role = Role.Create("superadmin", true, "system", clock);

        Assert.True(role.IsSystem);
        Assert.Equal("superadmin", role.Name);
    }

    [Fact]
    public void Create_WithNullName_ThrowsArgumentNullException()
    {
        var clock = new FakeClock();

        var ex = Assert.Throws<ArgumentNullException>(() =>
            Role.Create(null!, false, "system", clock));
        Assert.Contains("name", ex.ParamName!);
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsArgumentException()
    {
        var clock = new FakeClock();

        var ex = Assert.Throws<ArgumentException>(() =>
            Role.Create("", false, "system", clock));
        Assert.Contains("name", ex.ParamName!);
    }

    [Fact]
    public void Create_WithWhitespaceName_ThrowsArgumentException()
    {
        var clock = new FakeClock();

        var ex = Assert.Throws<ArgumentException>(() =>
            Role.Create("   ", false, "system", clock));
        Assert.Contains("name", ex.ParamName!);
    }

    [Fact]
    public void DefaultPolicy_IsRoleDefault()
    {
        Assert.Equal(DeletionPolicy.RoleDefault, Role.DefaultPolicy);
    }

    [Fact]
    public void Role_ExtendsAuditableEntity()
    {
        var clock = new FakeClock();
        var role = Role.Create("Editor", false, "system", clock);

        Assert.True(role is AuditableEntity);
    }

    [Fact]
    public void MarkDeleted_OnSystemRole_ThrowsSystemRoleProtectedException()
    {
        var clock = new FakeClock();
        var role = Role.Create("superadmin", true, "system", clock);

        var ex = Assert.Throws<SystemRoleProtectedException>(() =>
            role.MarkDeleted("admin", clock));
        Assert.Contains("system", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void MarkDeleted_ViaAuditableEntityReference_OnSystemRole_ThrowsSystemRoleProtectedException()
    {
        // Regression: casts to AuditableEntity must not bypass the system role deletion guard.
        var clock = new FakeClock();
        var role = Role.Create("superadmin", true, "system", clock);
        AuditableEntity entity = role;

        var ex = Assert.Throws<SystemRoleProtectedException>(() =>
            entity.MarkDeleted("admin", clock));
        Assert.Contains("system", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void MarkDeleted_OnNonSystemRole_Succeeds()
    {
        var clock = new FakeClock();
        var role = Role.Create("Custom", false, "system", clock);

        role.MarkDeleted("admin", clock);

        Assert.True(role.IsDeleted);
        Assert.Equal("admin", role.DeletedBy);
        Assert.Equal(clock.UtcNow, role.DeletedAt);
    }

    [Fact]
    public void AddPermission_WithValidPermission_AddsRolePermission()
    {
        var clock = new FakeClock();
        var role = Role.Create("Editor", false, "system", clock);
        var permission = Permission.Create(
            PermissionKey.Create("articles.edit"), "Edit articles", null, "system", clock);

        var rolePermission = role.AddPermission(permission, "admin", clock);

        Assert.Single(role.RolePermissions);
        Assert.Equal(role.Id, rolePermission.RoleId);
        Assert.Equal(permission.Id, rolePermission.PermissionId);
        Assert.Equal("admin", rolePermission.AssignedBy);
    }

    [Fact]
    public void AddPermission_DuplicatePermission_ThrowsInvalidOperationException()
    {
        var clock = new FakeClock();
        var role = Role.Create("Editor", false, "system", clock);
        var permission = Permission.Create(
            PermissionKey.Create("articles.edit"), "Edit articles", null, "system", clock);

        role.AddPermission(permission, "admin", clock);

        Assert.Throws<InvalidOperationException>(() =>
            role.AddPermission(permission, "admin", clock));
    }

    [Fact]
    public void RemovePermission_WithExistingPermission_RemovesIt()
    {
        var clock = new FakeClock();
        var role = Role.Create("Editor", false, "system", clock);
        var permission = Permission.Create(
            PermissionKey.Create("articles.edit"), "Edit articles", null, "system", clock);

        role.AddPermission(permission, "admin", clock);
        var removed = role.RemovePermission(permission.Id);

        Assert.True(removed);
        Assert.Empty(role.RolePermissions);
    }

    [Fact]
    public void RemovePermission_WithNonExistingPermission_ReturnsFalse()
    {
        var clock = new FakeClock();
        var role = Role.Create("Editor", false, "system", clock);

        var removed = role.RemovePermission(PermissionId.New());

        Assert.False(removed);
    }

    [Fact]
    public void CopyPermissionsTo_CopiesAllPermissionsToTarget()
    {
        var clock = new FakeClock();
        var source = Role.Create("Source", false, "system", clock);
        var target = Role.Create("Target", false, "system", clock);
        var p1 = Permission.Create(PermissionKey.Create("users.read"), "Read users", null, "system", clock);
        var p2 = Permission.Create(PermissionKey.Create("users.write"), "Write users", null, "system", clock);

        source.AddPermission(p1, "admin", clock);
        source.AddPermission(p2, "admin", clock);

        source.CopyPermissionsTo(target, "admin", clock);

        Assert.Equal(2, target.RolePermissions.Count);
    }

    [Fact]
    public void CopyPermissionsTo_IsPointInTime_SubsequentChangesDoNotPropagate()
    {
        var clock = new FakeClock();
        var source = Role.Create("Source", false, "system", clock);
        var target = Role.Create("Target", false, "system", clock);
        var p1 = Permission.Create(PermissionKey.Create("users.read"), "Read users", null, "system", clock);
        var p2 = Permission.Create(PermissionKey.Create("users.write"), "Write users", null, "system", clock);

        source.AddPermission(p1, "admin", clock);

        source.CopyPermissionsTo(target, "admin", clock);
        Assert.Single(target.RolePermissions);

        // Add another permission to source after copy
        source.AddPermission(p2, "admin", clock);

        // Target should NOT have the new permission
        Assert.Single(target.RolePermissions);
    }
}
