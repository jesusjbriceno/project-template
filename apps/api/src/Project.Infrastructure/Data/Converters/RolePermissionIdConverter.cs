using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Project.Domain.ValueObjects.Ids;

namespace Project.Infrastructure.Data.Converters;

/// <summary>
/// Converts composite RolePermissionId to/from a string representation "roleIdGuid_permissionIdGuid".
/// </summary>
public sealed class RolePermissionIdConverter : ValueConverter<RolePermissionId, string>
{
    public RolePermissionIdConverter()
        : base(
            id => $"{id.RoleId.Value:N}_{id.PermissionId.Value:N}",
            s => RolePermissionId.From(
                RoleId.From(Guid.Parse(s.Split('_')[0])),
                PermissionId.From(Guid.Parse(s.Split('_')[1]))))
    {
    }
}
