using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity configuration for <see cref="Permission"/>.
/// Maps to the "permissions" table with a unique Key index.
/// No soft-delete — Permission uses hard-delete semantics only.
/// </summary>
public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(p => p.Id);

        // Permission key must be unique
        builder.HasIndex(p => p.Key).IsUnique();

        // NO soft-delete query filter — Permission.DefaultPolicy.SoftDeleteEnabled == false

        // Audit fields
        builder.Property(p => p.CreatedAt);
        builder.Property(p => p.UpdatedAt);
        builder.Property(p => p.DeletedAt);
    }
}
