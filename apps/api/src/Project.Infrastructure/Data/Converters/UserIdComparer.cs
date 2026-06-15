using Microsoft.EntityFrameworkCore.ChangeTracking;
using Project.Domain.ValueObjects.Ids;

namespace Project.Infrastructure.Data.Converters;

public sealed class UserIdComparer : ValueComparer<UserId>
{
    public UserIdComparer()
        : base(
            (a, b) => a != null && b != null && a.Value == b.Value,
            id => id.Value.GetHashCode(),
            id => UserId.From(id.Value))
    {
    }
}
