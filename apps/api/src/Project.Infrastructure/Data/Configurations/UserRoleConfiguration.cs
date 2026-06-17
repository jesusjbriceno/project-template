using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity configuration for the <see cref="UserRole"/> junction table.
/// Maps to "user_roles" with composite primary key {UserId, RoleId}
/// and foreign keys to users and roles tables.
/// The UserRoleId convenience wrapper is ignored — identity is composite.
/// </summary>
public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");

        // Composite primary key — no synthetic surrogate
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });

        // Ignore the convenience UserRoleId wrapper (identity is the composite key)
        builder.Ignore(ur => ur.Id);

        // FK to Role — navigationless, Restrict delete ensures referential integrity
        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Assignment audit columns
        builder.Property(ur => ur.AssignedAt);
        builder.Property(ur => ur.AssignedBy).IsRequired();
    }
}
