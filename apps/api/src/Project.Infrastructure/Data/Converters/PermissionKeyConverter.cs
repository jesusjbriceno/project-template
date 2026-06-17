using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Project.Domain.ValueObjects;

namespace Project.Infrastructure.Data.Converters;

/// <summary>
/// Converts PermissionKey value object to/from its string Value.
/// </summary>
public sealed class PermissionKeyConverter : ValueConverter<PermissionKey, string>
{
    public PermissionKeyConverter()
        : base(key => key.Value, s => PermissionKey.Create(s))
    {
    }
}
