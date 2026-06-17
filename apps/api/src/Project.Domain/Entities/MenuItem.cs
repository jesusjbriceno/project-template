using Project.Domain.Common;
using Project.Domain.Errors;
using Project.Domain.Policies;
using Project.Domain.ValueObjects;
using Project.Domain.ValueObjects.Ids;

namespace Project.Domain.Entities;

/// <summary>
/// Represents a navigable menu item in a hierarchical tree.
/// Supports parent-child relationships via <see cref="ParentId"/> with cycle prevention.
/// Visibility may optionally declare required <see cref="PermissionKey"/> or <see cref="RoleId"/>.
/// Backend authorization remains authoritative — these are metadata hints for the UI.
/// </summary>
public sealed class MenuItem : AuditableEntity
{
    public MenuItemId Id { get; private set; }
    public string Label { get; private set; }
    public string? Icon { get; private set; }
    public string? Route { get; private set; }
    public MenuItemId? ParentId { get; private set; }
    public int SortOrder { get; private set; }
    public PermissionKey? RequiredPermissionKey { get; private set; }
    public RoleId? RequiredRoleId { get; private set; }
    public bool IsVisible { get; private set; }

    public static DeletionPolicy DefaultPolicy => DeletionPolicy.MenuItemDefault;

#pragma warning disable CS8618
    private MenuItem()
    {
        // Private parameterless constructor for EF Core materialization.
        // Properties (including audit fields from AuditableEntity) are set
        // via their private setters after construction.
    }
#pragma warning restore CS8618

    private MenuItem(
        MenuItemId id,
        string label,
        string? icon,
        string? route,
        MenuItemId? parentId,
        int sortOrder,
        PermissionKey? requiredPermissionKey,
        RoleId? requiredRoleId,
        bool isVisible)
    {
        Id = id;
        Label = label;
        Icon = icon;
        Route = route;
        ParentId = parentId;
        SortOrder = sortOrder;
        RequiredPermissionKey = requiredPermissionKey;
        RequiredRoleId = requiredRoleId;
        IsVisible = isVisible;
    }

    public static MenuItem Create(
        string label,
        string? icon,
        string? route,
        MenuItemId? parentId,
        int sortOrder,
        PermissionKey? requiredPermissionKey,
        RoleId? requiredRoleId,
        bool isVisible,
        string createdBy,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(label);
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Menu item label cannot be empty or whitespace.", nameof(label));
        if (sortOrder < 0)
            throw new ArgumentOutOfRangeException(nameof(sortOrder), sortOrder,
                "Sort order cannot be negative.");

        var item = new MenuItem(
            MenuItemId.New(), label, icon, route,
            parentId, sortOrder, requiredPermissionKey, requiredRoleId, isVisible);
        item.MarkCreated(createdBy, clock);
        return item;
    }

    /// <summary>
    /// Sets the parent of this menu item with cycle detection.
    /// Walks the ancestor chain through <paramref name="allItems"/> and throws
    /// <see cref="MenuCycleDetectedException"/> if this item appears as its own ancestor.
    /// Setting <paramref name="parentId"/> to null always succeeds (root node).
    /// </summary>
    /// <param name="parentId">The new parent ID, or null to make this a root item.</param>
    /// <param name="allItems">
    /// All menu items in the hierarchy (pre-loaded by the Application layer).
    /// Used to walk the ancestor chain without persistence access.
    /// </param>
    /// <exception cref="MenuCycleDetectedException">
    /// Thrown when setting the parent would create a cycle (this item is an ancestor
    /// of the proposed parent).
    /// </exception>
    public void SetParent(MenuItemId? parentId, IReadOnlyCollection<MenuItem> allItems)
    {
        if (parentId is null)
        {
            ParentId = null;
            return;
        }

        if (parentId.Equals(Id))
            throw new MenuCycleDetectedException(
                $"Cannot set '{Label}' as its own parent. " +
                "Self-referencing creates a cycle in the menu hierarchy.");

        // Walk ancestors of the proposed parent using Guid values to avoid
        // implicit operator confusion during comparison.
        var selfGuid = (Guid)Id;
        var visited = new HashSet<Guid>();
        MenuItemId currentId = parentId;
        while (true)
        {
            var currentGuid = (Guid)currentId;

            if (currentGuid == selfGuid)
                throw new MenuCycleDetectedException(
                    $"Cannot set parent of '{Label}' to create a cycle. " +
                    "The proposed parent is a descendant of this menu item.");

            if (!visited.Add(currentGuid))
                throw new MenuCycleDetectedException(
                    $"Existing cycle detected in menu hierarchy near item '{Label}'. " +
                    "The hierarchy is already corrupted.");

            var ancestor = allItems.FirstOrDefault(m => m.Id.Equals(currentId));
            if (ancestor is null || ancestor.ParentId is null)
                break;
            currentId = ancestor.ParentId;
        }

        ParentId = parentId;
    }

    /// <summary>
    /// Soft-deletes the menu item. Override point for future hierarchy consistency checks
    /// (e.g., reassign children before deletion).
    /// </summary>
    public void Delete(string deletedBy, IClock clock)
    {
        MarkDeleted(deletedBy, clock);
    }
}
