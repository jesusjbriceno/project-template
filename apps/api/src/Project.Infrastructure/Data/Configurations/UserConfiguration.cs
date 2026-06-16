using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity configuration for <see cref="User"/>.
/// Maps to the "users" table with a unique Email index and
/// soft-delete query filter (IsDeleted == false hidden by default).
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        // Email is unique at the database level
        builder.HasIndex(u => u.Email).IsUnique();

        // Soft-delete: hidden by default (DeletedAt is a mapped column, IsDeleted is a computed property)
        builder.HasQueryFilter(u => u.DeletedAt == null);

        // Navigation: UserRoles are mapped separately (Phase 3)
        builder.Ignore(u => u.UserRoles);

        // Audit fields: timestamptz via DateTimeOffset (Npgsql native mapping)
        builder.Property(u => u.CreatedAt);
        builder.Property(u => u.UpdatedAt);
        builder.Property(u => u.DeletedAt);
    }
}
