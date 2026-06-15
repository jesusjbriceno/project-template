using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Project.Domain.ValueObjects.Ids;

namespace Project.Infrastructure.Data.Converters;

public sealed class RoleIdConverter : ValueConverter<RoleId, Guid>
{
    public RoleIdConverter()
        : base(id => id.Value, guid => RoleId.From(guid))
    {
    }
}
