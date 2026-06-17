using Microsoft.EntityFrameworkCore;
using Project.Domain.Entities;
using Project.Domain.Policies;
using Project.Domain.ValueObjects;
using Project.Domain.ValueObjects.Ids;
using Project.Infrastructure.Data.Converters;
using Project.Infrastructure.Data.Interceptors;

namespace Project.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for PostgreSQL persistence.
/// Configured with NoTracking default, SplitQuery, Npgsql retry strategy,
/// and audit timestamp interceptor.
///
/// Entity configurations are applied via ConfigureConventions (value converters)
/// and OnModelCreating (detailed mappings deferred to Phase 2-3 IEntityTypeConfiguration classes).
/// </summary>
public sealed class ApplicationDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // NoTracking by default — configured in OnConfiguring via options
        base.ConfigureConventions(configurationBuilder);

        // Strongly-typed ID converters and comparers
        configurationBuilder.Properties<UserId>().HaveConversion<UserIdConverter, UserIdComparer>();
        configurationBuilder.Properties<RoleId>().HaveConversion<RoleIdConverter, RoleIdComparer>();
        configurationBuilder.Properties<PermissionId>().HaveConversion<PermissionIdConverter, PermissionIdComparer>();
        configurationBuilder.Properties<RefreshTokenId>().HaveConversion<RefreshTokenIdConverter, RefreshTokenIdComparer>();
        configurationBuilder.Properties<MenuItemId>().HaveConversion<MenuItemIdConverter, MenuItemIdComparer>();
        configurationBuilder.Properties<UserRoleId>().HaveConversion<UserRoleIdConverter, UserRoleIdComparer>();
        configurationBuilder.Properties<RolePermissionId>().HaveConversion<RolePermissionIdConverter, RolePermissionIdComparer>();

        // Value object converters
        configurationBuilder.Properties<Email>().HaveConversion<EmailConverter>();
        configurationBuilder.Properties<PermissionKey>().HaveConversion<PermissionKeyConverter>();
        configurationBuilder.Properties<DeletionPolicy>().HaveConversion<DeletionPolicyConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Entity-specific configurations (table names, indexes, FKs, soft-delete filters)
        // are applied in Phase 2-3 via IEntityTypeConfiguration<T> classes.
        // This method scans and applies them when they exist.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
