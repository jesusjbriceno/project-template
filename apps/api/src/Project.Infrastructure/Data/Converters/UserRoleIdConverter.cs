using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Project.Domain.ValueObjects.Ids;

namespace Project.Infrastructure.Data.Converters;

/// <summary>
/// Converts composite UserRoleId to/from a string representation "userIdGuid_roleIdGuid".
/// </summary>
public sealed class UserRoleIdConverter : ValueConverter<UserRoleId, string>
{
    public UserRoleIdConverter()
        : base(
            id => $"{id.UserId.Value:N}_{id.RoleId.Value:N}",
            s => UserRoleId.From(
                UserId.From(Guid.Parse(s.Split('_')[0])),
                RoleId.From(Guid.Parse(s.Split('_')[1]))))
    {
    }
}
