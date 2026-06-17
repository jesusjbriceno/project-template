using Project.Domain.Common;
using Project.Domain.ValueObjects.Ids;

namespace Project.Domain.Entities;

/// <summary>
/// Junction entity linking a Role to a Permission, capturing assignment audit metadata.
/// Uses a composite identity (RoleId + PermissionId) as its primary key domain type.
/// </summary>
public sealed class RolePermission
{
    public RolePermissionId Id { get; private set; }
    public RoleId RoleId { get; private set; }
    public PermissionId PermissionId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public string AssignedBy { get; private set; }

#pragma warning disable CS8618
    private RolePermission()
    {
        // Private parameterless constructor for EF Core materialization.
        // Properties are set via their private setters after construction.
    }
#pragma warning restore CS8618

    private RolePermission(RolePermissionId id, DateTimeOffset assignedAt, string assignedBy)
    {
        Id = id;
        RoleId = id.RoleId;
        PermissionId = id.PermissionId;
        AssignedAt = assignedAt;
        AssignedBy = assignedBy;
    }

    public static RolePermission Assign(RoleId roleId, PermissionId permissionId, string assignedBy, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(roleId);
        ArgumentNullException.ThrowIfNull(permissionId);

        var id = RolePermissionId.From(roleId, permissionId);
        return new RolePermission(id, clock.UtcNow, assignedBy);
    }
}
