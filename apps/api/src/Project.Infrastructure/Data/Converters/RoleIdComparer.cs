using Microsoft.EntityFrameworkCore.ChangeTracking;
using Project.Domain.ValueObjects.Ids;

namespace Project.Infrastructure.Data.Converters;

public sealed class RoleIdComparer : ValueComparer<RoleId>
{
    public RoleIdComparer()
        : base(
            (a, b) => a != null && b != null && a.Value == b.Value,
            id => id.Value.GetHashCode(),
            id => RoleId.From(id.Value))
    {
    }
}
