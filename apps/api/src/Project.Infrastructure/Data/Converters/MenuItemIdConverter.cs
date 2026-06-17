using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Project.Domain.ValueObjects.Ids;

namespace Project.Infrastructure.Data.Converters;

public sealed class MenuItemIdConverter : ValueConverter<MenuItemId, Guid>
{
    public MenuItemIdConverter()
        : base(id => id.Value, guid => MenuItemId.From(guid))
    {
    }
}
