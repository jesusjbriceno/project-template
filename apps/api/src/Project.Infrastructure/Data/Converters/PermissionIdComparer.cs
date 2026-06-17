using Microsoft.EntityFrameworkCore.ChangeTracking;
using Project.Domain.ValueObjects.Ids;

namespace Project.Infrastructure.Data.Converters;

public sealed class PermissionIdComparer : ValueComparer<PermissionId>
{
    public PermissionIdComparer()
        : base(
            (a, b) => a != null && b != null && a.Value == b.Value,
            id => id.Value.GetHashCode(),
            id => PermissionId.From(id.Value))
    {
    }
}
