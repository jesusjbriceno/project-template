using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity configuration for <see cref="RefreshToken"/>.
/// Maps to the "refresh_tokens" table with a unique TokenHash index,
/// FamilyId and ExpiresAt indexes for query performance.
/// No soft-delete filter — RefreshToken lifecycle is governed by
/// revocation and expiration, not soft/hard delete.
/// </summary>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(rt => rt.Id);

        // TokenHash must be unique — prevents duplicate token materialization
        builder.HasIndex(rt => rt.TokenHash).IsUnique();

        // FamilyId index for efficient family/session queries
        builder.HasIndex(rt => rt.FamilyId);

        // ExpiresAt index for efficient expiry cleanup queries
        builder.HasIndex(rt => rt.ExpiresAt);

        // NO soft-delete query filter — RefreshToken.DefaultPolicy.SoftDeleteEnabled == false
        // Revoked tokens remain visible for security audit and reuse detection.

        // Properties
        builder.Property(rt => rt.UserId).IsRequired();
        builder.Property(rt => rt.TokenHash).IsRequired();
        builder.Property(rt => rt.FamilyId);
        builder.Property(rt => rt.ExpiresAt);
        builder.Property(rt => rt.CreatedAt);
        builder.Property(rt => rt.RevokedAt);
        builder.Property(rt => rt.ReplacedByTokenHash);
    }
}
