using Project.Domain.Entities;
using Project.Domain.ValueObjects;
using Project.Domain.ValueObjects.Ids;
using Project.Domain.Common;
using Project.Domain.Policies;
using Project.Domain.Errors;

namespace Project.UnitTests.Entities;

public class MenuItemTests
{
    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow { get; init; } = new(2026, 6, 14, 10, 0, 0, TimeSpan.Zero);
    }

    [Fact]
    public void Create_WithValidLabel_CreatesMenuItem()
    {
        var clock = new FakeClock();

        var item = MenuItem.Create("Dashboard", null, null, null, 0,
            null, null, true, "system", clock);

        Assert.NotEqual(default, item.Id);
        Assert.Equal("Dashboard", item.Label);
        Assert.Null(item.Icon);
        Assert.Null(item.Route);
        Assert.Null(item.ParentId);
        Assert.Equal(0, item.SortOrder);
        Assert.Null(item.RequiredPermissionKey);
        Assert.Null(item.RequiredRoleId);
        Assert.True(item.IsVisible);
        Assert.Equal(clock.UtcNow, item.CreatedAt);
        Assert.Equal("system", item.CreatedBy);
    }

    [Fact]
    public void Create_WithAllFields_SetsAllProperties()
    {
        var clock = new FakeClock();
        var parentId = MenuItemId.New();
        var permissionKey = PermissionKey.Create("users.read");
        var roleId = RoleId.New();

        var item = MenuItem.Create("Users", "people-icon", "/users", parentId, 10,
            permissionKey, roleId, false, "admin", clock);

        Assert.Equal("Users", item.Label);
        Assert.Equal("people-icon", item.Icon);
        Assert.Equal("/users", item.Route);
        Assert.Equal(parentId, item.ParentId);
        Assert.Equal(10, item.SortOrder);
        Assert.Equal(permissionKey, item.RequiredPermissionKey);
        Assert.Equal(roleId, item.RequiredRoleId);
        Assert.False(item.IsVisible);
    }

    [Fact]
    public void Create_WithNullLabel_ThrowsArgumentNullException()
    {
        var clock = new FakeClock();
        var ex = Assert.Throws<ArgumentNullException>(() =>
            MenuItem.Create(null!, null, null, null, 0, null, null, true, "system", clock));
        Assert.Contains("label", ex.ParamName!);
    }

    [Fact]
    public void Create_WithEmptyLabel_ThrowsArgumentException()
    {
        var clock = new FakeClock();
        var ex = Assert.Throws<ArgumentException>(() =>
            MenuItem.Create("", null, null, null, 0, null, null, true, "system", clock));
        Assert.Contains("label", ex.ParamName!);
    }

    [Fact]
    public void Create_WithWhitespaceLabel_ThrowsArgumentException()
    {
        var clock = new FakeClock();
        var ex = Assert.Throws<ArgumentException>(() =>
            MenuItem.Create("   ", null, null, null, 0, null, null, true, "system", clock));
        Assert.Contains("label", ex.ParamName!);
    }

    [Fact]
    public void Create_WithNegativeSortOrder_ThrowsArgumentOutOfRangeException()
    {
        var clock = new FakeClock();
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            MenuItem.Create("Item", null, null, null, -1, null, null, true, "system", clock));
        Assert.Contains("sortOrder", ex.ParamName!);
    }

    [Fact]
    public void DefaultPolicy_IsMenuItemDefault()
    {
        Assert.Equal(DeletionPolicy.MenuItemDefault, MenuItem.DefaultPolicy);
    }

    [Fact]
    public void MenuItem_ExtendsAuditableEntity()
    {
        var clock = new FakeClock();
        var item = MenuItem.Create("Home", null, null, null, 0,
            null, null, true, "system", clock);

        Assert.True(item is AuditableEntity);
    }

    [Fact]
    public void SetParent_WithNullValue_MakesRoot()
    {
        var clock = new FakeClock();
        var child = MenuItem.Create("Child", null, null, MenuItemId.New(), 0,
            null, null, true, "system", clock);

        child.SetParent(null, [child]);

        Assert.Null(child.ParentId);
    }

    [Fact]
    public void SetParent_WithValidNewParent_SetsParentId()
    {
        var clock = new FakeClock();
        var parent = MenuItem.Create("Parent", null, null, null, 0,
            null, null, true, "system", clock);
        var child = MenuItem.Create("Child", null, null, null, 0,
            null, null, true, "system", clock);
        var allItems = new List<MenuItem> { parent, child };

        child.SetParent(parent.Id, allItems.AsReadOnly());

        Assert.Equal(parent.Id, child.ParentId);
    }

    [Fact]
    public void SetParent_SelfReference_ThrowsMenuCycleDetectedException()
    {
        var clock = new FakeClock();
        var item = MenuItem.Create("Item", null, null, null, 0,
            null, null, true, "system", clock);
        var allItems = new List<MenuItem> { item };

        var ex = Assert.Throws<MenuCycleDetectedException>(() =>
            item.SetParent(item.Id, allItems.AsReadOnly()));
        Assert.Contains("cycle", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void SetParent_DirectCycle_ThrowsMenuCycleDetectedException()
    {
        var clock = new FakeClock();
        var a = MenuItem.Create("A", null, null, null, 0,
            null, null, true, "system", clock);
        var b = MenuItem.Create("B", null, null, a.Id, 0,
            null, null, true, "system", clock);
        var allItems = new List<MenuItem> { a, b };

        // Setting A's parent to B creates cycle A→B→A
        var ex = Assert.Throws<MenuCycleDetectedException>(() =>
            a.SetParent(b.Id, allItems.AsReadOnly()));
        Assert.Contains("cycle", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void SetParent_ThreeLevelCycle_ThrowsMenuCycleDetectedException()
    {
        var clock = new FakeClock();
        var root = MenuItem.Create("Root", null, null, null, 0,
            null, null, true, "system", clock);
        var level1 = MenuItem.Create("Level1", null, null, root.Id, 0,
            null, null, true, "system", clock);
        var level2 = MenuItem.Create("Level2", null, null, level1.Id, 0,
            null, null, true, "system", clock);
        var allItems = new List<MenuItem> { root, level1, level2 };

        // Setting root's parent to level2 creates cycle root→level1→level2→root
        var ex = Assert.Throws<MenuCycleDetectedException>(() =>
            root.SetParent(level2.Id, allItems.AsReadOnly()));
        Assert.Contains("cycle", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void SetParent_ReassignLeafToSibling_NoCycle()
    {
        var clock = new FakeClock();
        var root = MenuItem.Create("Root", null, null, null, 0,
            null, null, true, "system", clock);
        var a = MenuItem.Create("A", null, null, root.Id, 0,
            null, null, true, "system", clock);
        var b = MenuItem.Create("B", null, null, root.Id, 1,
            null, null, true, "system", clock);
        var allItems = new List<MenuItem> { root, a, b };

        // Move B under A — valid, no cycle
        b.SetParent(a.Id, allItems.AsReadOnly());

        Assert.Equal(a.Id, b.ParentId); // Now root→A→B (was root→A, root→B)
    }

    [Fact]
    public void SetParent_MoveRootToNull_NoCycle()
    {
        var clock = new FakeClock();
        var root = MenuItem.Create("Root", null, null, null, 0,
            null, null, true, "system", clock);
        var child = MenuItem.Create("Child", null, null, root.Id, 0,
            null, null, true, "system", clock);
        var allItems = new List<MenuItem> { root, child };

        // Move root to null — always valid
        root.SetParent(null, allItems.AsReadOnly());

        Assert.Null(root.ParentId);
    }

    [Fact]
    public void Delete_SoftDeletesMenuItem()
    {
        var clock = new FakeClock();
        var item = MenuItem.Create("Item", null, null, null, 0,
            null, null, true, "system", clock);

        item.Delete("admin", clock);

        Assert.True(item.IsDeleted);
        Assert.Equal("admin", item.DeletedBy);
        Assert.Equal(clock.UtcNow, item.DeletedAt);
    }
}
