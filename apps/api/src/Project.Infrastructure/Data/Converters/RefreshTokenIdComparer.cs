using Microsoft.EntityFrameworkCore.ChangeTracking;
using Project.Domain.ValueObjects.Ids;

namespace Project.Infrastructure.Data.Converters;

public sealed class RefreshTokenIdComparer : ValueComparer<RefreshTokenId>
{
    public RefreshTokenIdComparer()
        : base(
            (a, b) => a != null && b != null && a.Value == b.Value,
            id => id.Value.GetHashCode(),
            id => RefreshTokenId.From(id.Value))
    {
    }
}
