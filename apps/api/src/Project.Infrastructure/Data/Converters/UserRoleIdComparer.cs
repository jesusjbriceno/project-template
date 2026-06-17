using Microsoft.EntityFrameworkCore.ChangeTracking;
using Project.Domain.ValueObjects.Ids;

namespace Project.Infrastructure.Data.Converters;

public sealed class UserRoleIdComparer : ValueComparer<UserRoleId>
{
    public UserRoleIdComparer()
        : base(
            (a, b) => a != null && b != null && a.Equals(b),
            id => HashCode.Combine(id.UserId.Value, id.RoleId.Value),
            id => UserRoleId.From(
                UserId.From(id.UserId.Value),
                RoleId.From(id.RoleId.Value)))
    {
    }
}
