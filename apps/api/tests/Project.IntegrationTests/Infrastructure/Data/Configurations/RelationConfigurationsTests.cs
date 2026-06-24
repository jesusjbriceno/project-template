using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Project.Domain.Common;
using Project.Domain.Entities;
using Project.Domain.ValueObjects;
using Project.Domain.ValueObjects.Ids;
using Project.Infrastructure.Data;

namespace Project.IntegrationTests.Infrastructure.Data.Configurations;

[Collection("Postgres")]
public sealed class RelationConfigurationsTests : IClassFixture<PostgresFixture>
{
    private static readonly UserId _testUserId = UserId.New();
    private readonly PostgresFixture _fixture;
    private readonly IClock _clock = new SystemClock();
    public RelationConfigurationsTests(PostgresFixture f) => _fixture = f;

    // ── UserRole ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UserRole_RoundTrip_PersistsCompositeKey()
    {
        using var s = _fixture.CreateServiceProvider().CreateScope();
        var ctx = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var u = CreateUser(ctx, "rtt");
        var r = CreateRole(ctx, "RTRole");
        var ur = UserRole.Assign(u.Id, r.Id, "a", _clock);
        ctx.UserRoles.Add(ur);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var got = await ctx.UserRoles.FirstOrDefaultAsync(x => x.UserId == u.Id && x.RoleId == r.Id);
        Assert.NotNull(got);
        Assert.Equal(u.Id, got!.UserId);
        Assert.Equal(r.Id, got.RoleId);
        Assert.NotEqual(default, got.AssignedAt);
    }

    [Fact]
    public async Task UserRole_CompositeKey_RejectsDuplicate()
    {
        using var s = _fixture.CreateServiceProvider().CreateScope();
        var ctx = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var u = CreateUser(ctx, "dup");
        var r = CreateRole(ctx, "DupRole");
        ctx.UserRoles.Add(UserRole.Assign(u.Id, r.Id, "a1", _clock));
        await ctx.SaveChangesAsync();

        using var s2 = _fixture.CreateServiceProvider().CreateScope();
        var ctx2 = s2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var u2 = await ctx2.Users.FirstAsync(x => x.Id == u.Id);
        var r2 = await ctx2.Roles.FirstAsync(x => x.Id == r.Id);
        ctx2.UserRoles.Add(UserRole.Assign(u2.Id, r2.Id, "a2", _clock));
        await Assert.ThrowsAsync<DbUpdateException>(() => ctx2.SaveChangesAsync());
    }

    // ── RolePermission ────────────────────────────────────────────────────

    [Fact]
    public async Task RolePermission_RoundTrip_PersistsCompositeKey()
    {
        using var s = _fixture.CreateServiceProvider().CreateScope();
        var ctx = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var r = CreateRole(ctx, "RPRole");
        var p = CreatePermission(ctx, "rptest.read");
        var rp = RolePermission.Assign(r.Id, p.Id, "a", _clock);
        ctx.RolePermissions.Add(rp);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var got = await ctx.RolePermissions.FirstOrDefaultAsync(x => x.RoleId == r.Id && x.PermissionId == p.Id);
        Assert.NotNull(got);
        Assert.Equal(r.Id, got!.RoleId);
        Assert.Equal(p.Id, got.PermissionId);
    }

    [Fact]
    public async Task RolePermission_CompositeKey_RejectsDuplicate()
    {
        using var s = _fixture.CreateServiceProvider().CreateScope();
        var ctx = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var r = CreateRole(ctx, "RPDup");
        var p = CreatePermission(ctx, "rpdup.key");
        ctx.RolePermissions.Add(RolePermission.Assign(r.Id, p.Id, "a1", _clock));
        await ctx.SaveChangesAsync();

        using var s2 = _fixture.CreateServiceProvider().CreateScope();
        var ctx2 = s2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var r2 = await ctx2.Roles.FirstAsync(x => x.Id == r.Id);
        var p2 = await ctx2.Permissions.FirstAsync(x => x.Id == p.Id);
        ctx2.RolePermissions.Add(RolePermission.Assign(r2.Id, p2.Id, "a2", _clock));
        await Assert.ThrowsAsync<DbUpdateException>(() => ctx2.SaveChangesAsync());
    }

    // ── Orphan FK rejection ───────────────────────────────────────────────

    [Fact]
    public async Task UserRole_NonexistentRoleId_ThrowsDbUpdateException()
    {
        using var s = _fixture.CreateServiceProvider().CreateScope();
        var ctx = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var u = CreateUser(ctx, "orphan");
        var bogusRoleId = RoleId.New();
        var ur = UserRole.Assign(u.Id, bogusRoleId, "a", _clock);
        ctx.UserRoles.Add(ur);

        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task RolePermission_NonexistentPermissionId_ThrowsDbUpdateException()
    {
        using var s = _fixture.CreateServiceProvider().CreateScope();
        var ctx = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var r = CreateRole(ctx, "OrphanPerm");
        var bogusPermissionId = PermissionId.New();
        var rp = RolePermission.Assign(r.Id, bogusPermissionId, "a", _clock);
        ctx.RolePermissions.Add(rp);

        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    // ── MenuItem ──────────────────────────────────────────────────────────

    [Fact]
    public async Task MenuItem_RoundTrip_WithSelfRefParent_AndSoftDelete()
    {
        using var s = _fixture.CreateServiceProvider().CreateScope();
        var ctx = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var parent = MenuItem.Create("Settings", "gear", "/settings",
            null, 10, null, null, true, "c", _clock);
        ctx.MenuItems.Add(parent);
        await ctx.SaveChangesAsync();

        var child = MenuItem.Create("Profile", "user", "/settings/profile",
            parent.Id, 1, PermissionKey.Create("admin.access"), RoleId.New(),
            true, "c", _clock);
        ctx.MenuItems.Add(child);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var got = await ctx.MenuItems.FirstOrDefaultAsync(m => m.Id == child.Id);
        Assert.NotNull(got);
        Assert.Equal("Profile", got!.Label);
        Assert.Equal(parent.Id, got.ParentId);
        Assert.NotNull(got.RequiredPermissionKey);
        Assert.NotNull(got.RequiredRoleId);
        Assert.NotEqual(default, got.CreatedAt);
    }

    [Fact]
    public async Task MenuItem_SoftDeleted_HiddenByDefault()
    {
        using var s = _fixture.CreateServiceProvider().CreateScope();
        var ctx = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var item = MenuItem.Create("Old", null, "/old",
            null, 99, null, null, false, "c", _clock);
        item.Delete("d", _clock);
        ctx.MenuItems.Add(item);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        Assert.Null(await ctx.MenuItems.FirstOrDefaultAsync(m => m.Id == item.Id));
    }

    [Fact]
    public async Task RefreshToken_Revoked_StillVisible_NoSoftDelete()
    {
        using var s = _fixture.CreateServiceProvider().CreateScope();
        var ctx = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var token = RefreshToken.Create(_testUserId, new string('c', 64), Guid.NewGuid(),
            _clock.UtcNow.AddDays(7), _clock);
        token.Revoke(_clock);
        ctx.RefreshTokens.Add(token);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var got = await ctx.RefreshTokens.FirstOrDefaultAsync(rt => rt.Id == token.Id);
        Assert.NotNull(got);
        Assert.True(got!.IsRevoked);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private User CreateUser(ApplicationDbContext ctx, string suffix)
    {
        var u = User.Create(Email.Create($"u{suffix}@t.com"), "h", "c", _clock);
        ctx.Users.Add(u);
        ctx.SaveChanges();
        return u;
    }

    private Role CreateRole(ApplicationDbContext ctx, string name)
    {
        var r = Role.Create(name, false, "c", _clock);
        ctx.Roles.Add(r);
        ctx.SaveChanges();
        return r;
    }

    private Permission CreatePermission(ApplicationDbContext ctx, string key)
    {
        var p = Permission.Create(PermissionKey.Create(key), "desc", null, "c", _clock);
        ctx.Permissions.Add(p);
        ctx.SaveChanges();
        return p;
    }
}
