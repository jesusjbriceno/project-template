using Microsoft.EntityFrameworkCore.ChangeTracking;
using Project.Domain.ValueObjects.Ids;

namespace Project.Infrastructure.Data.Converters;

public sealed class MenuItemIdComparer : ValueComparer<MenuItemId>
{
    public MenuItemIdComparer()
        : base(
            (a, b) => a != null && b != null && a.Value == b.Value,
            id => id.Value.GetHashCode(),
            id => MenuItemId.From(id.Value))
    {
    }
}
