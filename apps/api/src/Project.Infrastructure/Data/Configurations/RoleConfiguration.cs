using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity configuration for <see cref="Role"/>.
/// Maps to the "roles" table with a unique Name index and
/// soft-delete query filter (IsDeleted == false hidden by default).
/// </summary>
public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);

        // Role name must be unique
        builder.HasIndex(r => r.Name).IsUnique();

        // Soft-delete: hidden by default (DeletedAt is a mapped column, IsDeleted is a computed property)
        builder.HasQueryFilter(r => r.DeletedAt == null);

        // Navigation: RolePermissions are mapped separately (Phase 3)
        builder.Ignore(r => r.RolePermissions);

        // Audit fields
        builder.Property(r => r.CreatedAt);
        builder.Property(r => r.UpdatedAt);
        builder.Property(r => r.DeletedAt);
    }
}
