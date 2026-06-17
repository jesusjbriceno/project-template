using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Project.Domain.ValueObjects.Ids;

namespace Project.Infrastructure.Data.Converters;

public sealed class UserIdConverter : ValueConverter<UserId, Guid>
{
    public UserIdConverter()
        : base(id => id.Value, guid => UserId.From(guid))
    {
    }
}
