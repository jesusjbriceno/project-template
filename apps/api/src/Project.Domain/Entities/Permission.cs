using Project.Domain.Common;
using Project.Domain.Policies;
using Project.Domain.ValueObjects;
using Project.Domain.ValueObjects.Ids;

namespace Project.Domain.Entities;

/// <summary>
/// Functionality-defined permission catalog entry (not UI-administered).
/// Identified by PermissionKey; carries Description and optional Category.
/// Hard-delete only — no soft-delete semantics.
/// </summary>
public sealed class Permission : AuditableEntity
{
    public PermissionId Id { get; private set; }
    public PermissionKey Key { get; private set; }
    public string Description { get; private set; }
    public string? Category { get; private set; }

    public static DeletionPolicy DefaultPolicy => DeletionPolicy.PermissionDefault;

    private Permission(PermissionId id, PermissionKey key, string description, string? category)
    {
        Id = id;
        Key = key;
        Description = description;
        Category = category;
    }

    public static Permission Create(PermissionKey key, string description, string? category, string createdBy, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(key);

        var permission = new Permission(PermissionId.New(), key, description, category);
        permission.MarkCreated(createdBy, clock);
        return permission;
    }
}
