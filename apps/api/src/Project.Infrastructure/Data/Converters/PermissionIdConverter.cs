using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Project.Domain.ValueObjects.Ids;

namespace Project.Infrastructure.Data.Converters;

public sealed class PermissionIdConverter : ValueConverter<PermissionId, Guid>
{
    public PermissionIdConverter()
        : base(id => id.Value, guid => PermissionId.From(guid))
    {
    }
}
