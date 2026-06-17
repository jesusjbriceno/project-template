using Microsoft.EntityFrameworkCore.ChangeTracking;
using Project.Domain.ValueObjects.Ids;

namespace Project.Infrastructure.Data.Converters;

public sealed class RolePermissionIdComparer : ValueComparer<RolePermissionId>
{
    public RolePermissionIdComparer()
        : base(
            (a, b) => a != null && b != null && a.Equals(b),
            id => HashCode.Combine(id.RoleId.Value, id.PermissionId.Value),
            id => RolePermissionId.From(
                RoleId.From(id.RoleId.Value),
                PermissionId.From(id.PermissionId.Value)))
    {
    }

}
