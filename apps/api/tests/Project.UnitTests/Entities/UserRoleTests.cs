using Project.Domain.Entities;
using Project.Domain.ValueObjects.Ids;
using Project.Domain.Common;

namespace Project.UnitTests.Entities;

public class UserRoleTests
{
    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow { get; init; } = new(2026, 6, 12, 10, 0, 0, TimeSpan.Zero);
    }

    [Fact]
    public void Assign_WithValidIds_SetsCompositeIdAndAudit()
    {
        var clock = new FakeClock();
        var userId = UserId.New();
        var roleId = RoleId.New();

        var ur = UserRole.Assign(userId, roleId, "admin", clock);

        Assert.Equal(userId, ur.UserId);
        Assert.Equal(roleId, ur.RoleId);
        Assert.Equal("admin", ur.AssignedBy);
        Assert.Equal(clock.UtcNow, ur.AssignedAt);
    }

    [Fact]
    public void Id_ReturnsUserRoleId_Composite()
    {
        var clock = new FakeClock();
        var userId = UserId.New();
        var roleId = RoleId.New();

        var ur = UserRole.Assign(userId, roleId, "admin", clock);

        Assert.Equal(userId, ur.Id.UserId);
        Assert.Equal(roleId, ur.Id.RoleId);
    }

    [Fact]
    public void Assign_WithNullUserId_ThrowsArgumentNullException()
    {
        var clock = new FakeClock();

        var ex = Assert.Throws<ArgumentNullException>(() =>
            UserRole.Assign(null!, RoleId.New(), "admin", clock));
        Assert.Contains("userId", ex.ParamName!);
    }

    [Fact]
    public void Assign_WithNullRoleId_ThrowsArgumentNullException()
    {
        var clock = new FakeClock();

        var ex = Assert.Throws<ArgumentNullException>(() =>
            UserRole.Assign(UserId.New(), null!, "admin", clock));
        Assert.Contains("roleId", ex.ParamName!);
    }

    [Fact]
    public void TwoAssignments_WithSameUserAndRole_HaveEqualIds()
    {
        var clock = new FakeClock();
        var userId = UserId.From(Guid.NewGuid());
        var roleId = RoleId.From(Guid.NewGuid());

        var ur1 = UserRole.Assign(userId, roleId, "admin", clock);
        var ur2 = UserRole.Assign(userId, roleId, "admin", clock);

        Assert.Equal(ur1.Id, ur2.Id);
    }
}
