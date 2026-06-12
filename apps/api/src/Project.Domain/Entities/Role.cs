using System.Collections.ObjectModel;
using Project.Domain.Common;
using Project.Domain.Errors;
using Project.Domain.Policies;
using Project.Domain.ValueObjects.Ids;

namespace Project.Domain.Entities;

/// <summary>
/// UI-administrable permission grouping.
/// System roles (IsSystem=true) are protected from deletion.
/// Permission copying is point-in-time only — no inheritance coupling.
/// </summary>
public sealed class Role : AuditableEntity
{
    private readonly List<RolePermission> _rolePermissions = new();

    public RoleId Id { get; private set; }
    public string Name { get; private set; }
    public bool IsSystem { get; private set; }
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    public static DeletionPolicy DefaultPolicy => DeletionPolicy.RoleDefault;

    private Role(RoleId id, string name, bool isSystem)
    {
        Id = id;
        Name = name;
        IsSystem = isSystem;
    }

    public static Role Create(string name, bool isSystem, string createdBy, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be empty or whitespace.", nameof(name));

        var role = new Role(RoleId.New(), name, isSystem);
        role.MarkCreated(createdBy, clock);
        return role;
    }

    /// <summary>
    /// Adds a permission to this role. Duplicate permission assignments are rejected.
    /// </summary>
    public RolePermission AddPermission(Permission permission, string assignedBy, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(permission);

        if (_rolePermissions.Any(rp => rp.PermissionId == permission.Id))
            throw new InvalidOperationException(
                $"Permission '{permission.Key}' is already assigned to role '{Name}'.");

        var rolePermission = RolePermission.Assign(Id, permission.Id, assignedBy, clock);
        _rolePermissions.Add(rolePermission);
        return rolePermission;
    }

    /// <summary>
    /// Removes a permission from this role by PermissionId.
    /// Returns true if the permission was found and removed, false otherwise.
    /// </summary>
    public bool RemovePermission(PermissionId permissionId)
    {
        var existing = _rolePermissions.FirstOrDefault(rp => rp.PermissionId == permissionId);
        if (existing is null) return false;
        _rolePermissions.Remove(existing);
        return true;
    }

    /// <summary>
    /// Copies all permissions from a source role to this target role.
    /// This is point-in-time only — subsequent source changes do NOT propagate.
    /// </summary>
    public void CopyPermissionsTo(Role target, string assignedBy, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(target);

        foreach (var rp in _rolePermissions)
        {
            // Skip if target already has this permission
            if (target._rolePermissions.Any(t => t.PermissionId == rp.PermissionId))
                continue;

            var copy = RolePermission.Assign(target.Id, rp.PermissionId, assignedBy, clock);
            target._rolePermissions.Add(copy);
        }
    }

    /// <summary>
    /// Overrides soft-delete to protect system roles from deletion.
    /// Protected — external callers must use <see cref="Delete"/> which routes through this guard.
    /// </summary>
    protected override void MarkDeleted(string deletedBy, IClock clock)
    {
        if (IsSystem)
            throw new SystemRoleProtectedException(
                $"System role '{Name}' cannot be deleted.");
        base.MarkDeleted(deletedBy, clock);
    }

    /// <summary>
    /// Soft-deletes the role. System roles (IsSystem=true) are protected and will throw
    /// <see cref="SystemRoleProtectedException"/>.
    /// </summary>
    public void Delete(string deletedBy, IClock clock)
    {
        MarkDeleted(deletedBy, clock);
    }
}
