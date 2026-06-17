using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity configuration for the <see cref="RolePermission"/> junction table.
/// Maps to "role_permissions" with composite primary key {RoleId, PermissionId}
/// and foreign keys to roles and permissions tables.
/// The RolePermissionId convenience wrapper is ignored — identity is composite.
/// </summary>
public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");

        // Composite primary key — no synthetic surrogate
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        // Ignore the convenience RolePermissionId wrapper (identity is the composite key)
        builder.Ignore(rp => rp.Id);

        // FK to Permission — navigationless, Restrict delete ensures referential integrity
        builder.HasOne<Permission>()
            .WithMany()
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Assignment audit columns
        builder.Property(rp => rp.AssignedAt);
        builder.Property(rp => rp.AssignedBy).IsRequired();
    }
}
