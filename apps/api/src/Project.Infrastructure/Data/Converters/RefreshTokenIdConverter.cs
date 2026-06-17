using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Project.Domain.ValueObjects.Ids;

namespace Project.Infrastructure.Data.Converters;

public sealed class RefreshTokenIdConverter : ValueConverter<RefreshTokenId, Guid>
{
    public RefreshTokenIdConverter()
        : base(id => id.Value, guid => RefreshTokenId.From(guid))
    {
    }
}
