using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity configuration for <see cref="MenuItem"/>.
/// Maps to the "menu_items" table with a self-referencing foreign key
/// on ParentId and a soft-delete query filter (DeletedAt == null).
/// </summary>
public sealed class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("menu_items");

        builder.HasKey(m => m.Id);

        // Self-referencing FK: nullable ParentId → menu_items.Id
        builder.HasOne<MenuItem>()
            .WithMany()
            .HasForeignKey(m => m.ParentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Soft-delete: hidden by default (DeletedAt is a mapped column, IsDeleted is computed)
        builder.HasQueryFilter(m => m.DeletedAt == null);

        // Audit fields from AuditableEntity
        builder.Property(m => m.CreatedAt);
        builder.Property(m => m.UpdatedAt);
        builder.Property(m => m.DeletedAt);
    }
}
