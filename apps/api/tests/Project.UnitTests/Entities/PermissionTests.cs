using Project.Domain.Entities;
using Project.Domain.ValueObjects;
using Project.Domain.ValueObjects.Ids;
using Project.Domain.Common;
using Project.Domain.Policies;
using Project.Domain.Errors;

namespace Project.UnitTests.Entities;

public class PermissionTests
{
    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow { get; init; } = new(2026, 6, 12, 10, 0, 0, TimeSpan.Zero);
    }

    [Fact]
    public void Create_WithValidKey_AssignsIdAndKeyAndAuditFields()
    {
        var clock = new FakeClock();
        var key = PermissionKey.Create("users.read");

        var permission = Permission.Create(key, "Can read users", "Users", "system", clock);

        Assert.NotEqual(default, permission.Id);
        Assert.Equal(key, permission.Key);
        Assert.Equal("Can read users", permission.Description);
        Assert.Equal("Users", permission.Category);
        Assert.Equal(clock.UtcNow, permission.CreatedAt);
        Assert.Equal("system", permission.CreatedBy);
    }

    [Fact]
    public void Create_WithValidKey_DescriptionDefaultsToKeyValue()
    {
        var clock = new FakeClock();
        var key = PermissionKey.Create("reports.export");

        var permission = Permission.Create(key, "Export reports", null, "system", clock);

        Assert.Equal("Export reports", permission.Description);
        Assert.Null(permission.Category);
    }

    [Fact]
    public void Create_WithNullKey_ThrowsArgumentNullException()
    {
        var clock = new FakeClock();

        var ex = Assert.Throws<ArgumentNullException>(() =>
            Permission.Create(null!, "desc", "cat", "system", clock));
        Assert.Contains("key", ex.ParamName!);
    }

    [Fact]
    public void Id_AfterCreate_IsUnique()
    {
        var clock = new FakeClock();
        var key = PermissionKey.Create("users.read");

        var p1 = Permission.Create(key, "desc", null, "system", clock);
        var p2 = Permission.Create(key, "desc", null, "system", clock);

        Assert.NotEqual(p1.Id, p2.Id);
    }

    [Fact]
    public void DefaultPolicy_IsPermissionDefault()
    {
        Assert.Equal(DeletionPolicy.PermissionDefault, Permission.DefaultPolicy);
    }

    [Fact]
    public void Permission_ExtendsAuditableEntity()
    {
        var clock = new FakeClock();
        var key = PermissionKey.Create("users.read");

        var permission = Permission.Create(key, "desc", null, "system", clock);

        Assert.True(permission is AuditableEntity);
        Assert.False(permission.IsDeleted);
    }
}
