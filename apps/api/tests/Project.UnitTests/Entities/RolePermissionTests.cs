using Project.Domain.Entities;
using Project.Domain.ValueObjects.Ids;
using Project.Domain.Common;

namespace Project.UnitTests.Entities;

public class RolePermissionTests
{
    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow { get; init; } = new(2026, 6, 12, 10, 0, 0, TimeSpan.Zero);
    }

    [Fact]
    public void Assign_WithValidIds_SetsCompositeIdAndAudit()
    {
        var clock = new FakeClock();
        var roleId = RoleId.New();
        var permissionId = PermissionId.New();

        var rp = RolePermission.Assign(roleId, permissionId, "admin", clock);

        Assert.Equal(roleId, rp.RoleId);
        Assert.Equal(permissionId, rp.PermissionId);
        Assert.Equal("admin", rp.AssignedBy);
        Assert.Equal(clock.UtcNow, rp.AssignedAt);
    }

    [Fact]
    public void Id_ReturnsRolePermissionId_Composite()
    {
        var clock = new FakeClock();
        var roleId = RoleId.New();
        var permissionId = PermissionId.New();

        var rp = RolePermission.Assign(roleId, permissionId, "admin", clock);

        Assert.Equal(roleId, rp.Id.RoleId);
        Assert.Equal(permissionId, rp.Id.PermissionId);
    }

    [Fact]
    public void Assign_WithNullRoleId_ThrowsArgumentNullException()
    {
        var clock = new FakeClock();

        var ex = Assert.Throws<ArgumentNullException>(() =>
            RolePermission.Assign(null!, PermissionId.New(), "admin", clock));
        Assert.Contains("roleId", ex.ParamName!);
    }

    [Fact]
    public void Assign_WithNullPermissionId_ThrowsArgumentNullException()
    {
        var clock = new FakeClock();

        var ex = Assert.Throws<ArgumentNullException>(() =>
            RolePermission.Assign(RoleId.New(), null!, "admin", clock));
        Assert.Contains("permissionId", ex.ParamName!);
    }

    [Fact]
    public void TwoAssignments_WithSameRoleAndPermission_HaveEqualIds()
    {
        var clock = new FakeClock();
        var roleId = RoleId.From(Guid.NewGuid());
        var permissionId = PermissionId.From(Guid.NewGuid());

        var rp1 = RolePermission.Assign(roleId, permissionId, "admin", clock);
        var rp2 = RolePermission.Assign(roleId, permissionId, "admin", clock);

        Assert.Equal(rp1.Id, rp2.Id);
    }
}
