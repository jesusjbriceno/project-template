using Project.Domain.Common;
using Project.Domain.ValueObjects.Ids;

namespace Project.Domain.Entities;

/// <summary>
/// Junction entity linking a User to a Role, capturing assignment audit metadata.
/// Uses a composite identity (UserId + RoleId) as its primary key domain type.
/// Junction entities carry assignment-specific audit fields (AssignedAt, AssignedBy)
/// instead of full AuditableEntity lifecycle — they exist only while the association is active.
/// </summary>
public sealed class UserRole
{
    public UserRoleId Id { get; private set; }
    public UserId UserId { get; private set; }
    public RoleId RoleId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public string AssignedBy { get; private set; }

    private UserRole(UserRoleId id, DateTimeOffset assignedAt, string assignedBy)
    {
        Id = id;
        UserId = id.UserId;
        RoleId = id.RoleId;
        AssignedAt = assignedAt;
        AssignedBy = assignedBy;
    }

    public static UserRole Assign(UserId userId, RoleId roleId, string assignedBy, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(roleId);

        var id = UserRoleId.From(userId, roleId);
        return new UserRole(id, clock.UtcNow, assignedBy);
    }
}
