using Project.Application.Abstractions.Persistence;
using Project.Domain.Entities;
using Project.Domain.ValueObjects;
using Project.Domain.ValueObjects.Ids;

namespace Project.ApplicationTests.Abstractions.Persistence;

/// <summary>
/// Compile-time and runtime contract proof: verifies that per-aggregate repository
/// interfaces expose async CRUD via shared <see cref="IBaseRepository{TEntity,TId}"/>,
/// use Domain entity types with strongly-typed IDs, and support invariant pre-loading
/// and aggregate-specific query methods. Hand-rolled stubs — no Moq dependency.
/// </summary>
public sealed class RepositoryContractTests
{
    // ─────────────── Stub implementations ───────────────

    private sealed class UserRepositoryStub : IUserRepository
    {
        public Task<User?> GetByIdAsync(UserId id, bool includeDeleted = false, CancellationToken ct = default) =>
            Task.FromResult<User?>(null);

        public Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default) =>
            Task.FromResult<User?>(null);

        public Task<bool> ExistsAsync(UserId id, CancellationToken ct = default) =>
            Task.FromResult(false);

        public Task AddAsync(User user, CancellationToken ct = default) =>
            Task.CompletedTask;

        public void Update(User entity) { }

        public void Delete(User entity) { }

        public Task<PagedResult<User>> GetPagedAsync(PageRequest request, bool includeDeleted = false, CancellationToken ct = default) =>
            Task.FromResult(new PagedResult<User>(Array.Empty<User>(), TotalCount: 0, request.Page, request.PageSize));

        public Task<IReadOnlyCollection<User>> GetActiveSuperadminsAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyCollection<User>>(Array.Empty<User>());
    }

    private sealed class RoleRepositoryStub : IRoleRepository
    {
        public Task<Role?> GetByIdAsync(RoleId id, bool includeDeleted = false, CancellationToken ct = default) =>
            Task.FromResult<Role?>(null);

        public Task<bool> ExistsAsync(RoleId id, CancellationToken ct = default) =>
            Task.FromResult(false);

        public Task AddAsync(Role role, CancellationToken ct = default) =>
            Task.CompletedTask;

        public void Update(Role entity) { }

        public void Delete(Role entity) { }

        public Task<PagedResult<Role>> GetPagedAsync(PageRequest request, bool includeDeleted = false, CancellationToken ct = default) =>
            Task.FromResult(new PagedResult<Role>(Array.Empty<Role>(), TotalCount: 0, request.Page, request.PageSize));

        public Task<IReadOnlyCollection<Role>> GetSystemRolesAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyCollection<Role>>(Array.Empty<Role>());
    }

    private sealed class PermissionRepositoryStub : IPermissionRepository
    {
        public Task<Permission?> GetByIdAsync(PermissionId id, bool includeDeleted = false, CancellationToken ct = default) =>
            Task.FromResult<Permission?>(null);

        public Task<Permission?> GetByKeyAsync(PermissionKey key, CancellationToken ct = default) =>
            Task.FromResult<Permission?>(null);

        public Task<bool> ExistsByKeyAsync(PermissionKey key, CancellationToken ct = default) =>
            Task.FromResult(false);

        public Task AddAsync(Permission permission, CancellationToken ct = default) =>
            Task.CompletedTask;

        public void Update(Permission entity) { }

        public void Delete(Permission entity) { }

        public Task<PagedResult<Permission>> GetPagedAsync(PageRequest request, bool includeDeleted = false, CancellationToken ct = default) =>
            Task.FromResult(new PagedResult<Permission>(Array.Empty<Permission>(), TotalCount: 0, request.Page, request.PageSize));
    }

    private sealed class RefreshTokenRepositoryStub : IRefreshTokenRepository
    {
        public Task<RefreshToken?> GetByIdAsync(RefreshTokenId id, bool includeDeleted = false, CancellationToken ct = default) =>
            Task.FromResult<RefreshToken?>(null);

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
            Task.FromResult<RefreshToken?>(null);

        public Task AddAsync(RefreshToken token, CancellationToken ct = default) =>
            Task.CompletedTask;

        public void Update(RefreshToken entity) { }

        public void Delete(RefreshToken entity) { }

        public Task<PagedResult<RefreshToken>> GetPagedAsync(PageRequest request, bool includeDeleted = false, CancellationToken ct = default) =>
            Task.FromResult(new PagedResult<RefreshToken>(Array.Empty<RefreshToken>(), TotalCount: 0, request.Page, request.PageSize));

        public Task RevokeFamilyAsync(Guid familyId, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyCollection<RefreshToken>> GetActiveByFamilyIdAsync(
            Guid familyId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyCollection<RefreshToken>>(Array.Empty<RefreshToken>());
    }

    private sealed class MenuItemRepositoryStub : IMenuItemRepository
    {
        public Task<MenuItem?> GetByIdAsync(MenuItemId id, bool includeDeleted = false, CancellationToken ct = default) =>
            Task.FromResult<MenuItem?>(null);

        public Task AddAsync(MenuItem item, CancellationToken ct = default) =>
            Task.CompletedTask;

        public void Update(MenuItem entity) { }

        public void Delete(MenuItem entity) { }

        public Task<PagedResult<MenuItem>> GetPagedAsync(PageRequest request, bool includeDeleted = false, CancellationToken ct = default) =>
            Task.FromResult(new PagedResult<MenuItem>(Array.Empty<MenuItem>(), TotalCount: 0, request.Page, request.PageSize));

        public Task<IReadOnlyCollection<MenuItem>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyCollection<MenuItem>>(Array.Empty<MenuItem>());

        public Task<IReadOnlyCollection<MenuItem>> GetChildrenAsync(
            MenuItemId parentId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyCollection<MenuItem>>(Array.Empty<MenuItem>());
    }

    // ─────────────── UserRepository tests ───────────────

    [Fact]
    public async Task UserRepository_GetByIdAsync_ReturnsUserOrNull_AndAcceptsCancellationToken()
    {
        IUserRepository repo = new UserRepositoryStub();
        var id = UserId.New();

        User? result = await repo.GetByIdAsync(id, cancellationToken: CancellationToken.None);

        Assert.Null(result); // stub returns null — proves signature compiles
    }

    [Fact]
    public async Task UserRepository_GetActiveSuperadminsAsync_ReturnsCollection()
    {
        IUserRepository repo = new UserRepositoryStub();

        IReadOnlyCollection<User> superadmins = await repo.GetActiveSuperadminsAsync(CancellationToken.None);

        Assert.NotNull(superadmins);
        Assert.Empty(superadmins);
    }

    [Fact]
    public async Task UserRepository_AddAsync_AcceptsUserAndCancellationToken()
    {
        IUserRepository repo = new UserRepositoryStub();
        var user = User.Create(
            Email.Create("test@example.com"),
            "hashed-password-placeholder",
            "system",
            DummyClock.Instance);

        // Must not throw — proves AddAsync signature compiles and accepts CancellationToken
        await repo.AddAsync(user, CancellationToken.None);
    }

    [Fact]
    public async Task UserRepository_ExistsAsync_ReturnsBool()
    {
        IUserRepository repo = new UserRepositoryStub();

        bool exists = await repo.ExistsAsync(UserId.New(), CancellationToken.None);

        Assert.False(exists); // stub returns false
    }

    // ─────────────── RoleRepository tests ───────────────

    [Fact]
    public async Task RoleRepository_GetByIdAsync_UsesRoleId()
    {
        IRoleRepository repo = new RoleRepositoryStub();

        Role? result = await repo.GetByIdAsync(RoleId.New(), cancellationToken: CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task RoleRepository_GetSystemRolesAsync_ReturnsCollection()
    {
        IRoleRepository repo = new RoleRepositoryStub();

        IReadOnlyCollection<Role> roles = await repo.GetSystemRolesAsync(CancellationToken.None);

        Assert.NotNull(roles);
        Assert.Empty(roles);
    }

    // ─────────────── PermissionRepository tests ───────────────

    [Fact]
    public async Task PermissionRepository_GetByKeyAsync_UsesPermissionKey()
    {
        IPermissionRepository repo = new PermissionRepositoryStub();
        var key = PermissionKey.Create("users.read");

        Permission? result = await repo.GetByKeyAsync(key, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task PermissionRepository_ExistsByKeyAsync_ReturnsBool()
    {
        IPermissionRepository repo = new PermissionRepositoryStub();

        bool exists = await repo.ExistsByKeyAsync(PermissionKey.Create("admin.access"), CancellationToken.None);

        Assert.False(exists);
    }

    // ─────────────── RefreshTokenRepository tests ───────────────

    [Fact]
    public async Task RefreshTokenRepository_GetByTokenHashAsync_UsesStringHash()
    {
        IRefreshTokenRepository repo = new RefreshTokenRepositoryStub();
        var hash = "a".PadRight(64, '0'); // valid-length hex placeholder

        RefreshToken? result = await repo.GetByTokenHashAsync(hash, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshTokenRepository_RevokeFamilyAsync_AcceptsGuidAndCancellationToken()
    {
        IRefreshTokenRepository repo = new RefreshTokenRepositoryStub();

        await repo.RevokeFamilyAsync(Guid.NewGuid(), CancellationToken.None);

        // Must not throw — proves signature compiles
    }

    [Fact]
    public async Task RefreshTokenRepository_GetActiveByFamilyIdAsync_ReturnsCollection()
    {
        IRefreshTokenRepository repo = new RefreshTokenRepositoryStub();

        IReadOnlyCollection<RefreshToken> tokens =
            await repo.GetActiveByFamilyIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.NotNull(tokens);
        Assert.Empty(tokens);
    }

    // ─────────────── MenuItemRepository tests ───────────────

    [Fact]
    public async Task MenuItemRepository_GetAllAsync_ReturnsFullHierarchy()
    {
        IMenuItemRepository repo = new MenuItemRepositoryStub();

        IReadOnlyCollection<MenuItem> items = await repo.GetAllAsync(CancellationToken.None);

        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public async Task MenuItemRepository_GetChildrenAsync_ReturnsDirectChildren()
    {
        IMenuItemRepository repo = new MenuItemRepositoryStub();

        IReadOnlyCollection<MenuItem> children =
            await repo.GetChildrenAsync(MenuItemId.New(), CancellationToken.None);

        Assert.NotNull(children);
        Assert.Empty(children);
    }
}

// ─────────────── IBaseRepository contract tests ───────────────

/// <summary>
/// Compile-time contract proof: verifies that <see cref="IBaseRepository{TEntity,TId}"/>
/// exposes async CRUD, void Update/Delete, paginated query, and accepts strongly-typed IDs.
/// Hand-rolled stub — no Moq dependency.
/// </summary>
public sealed class BaseRepositoryContractTests
{
    private sealed class TestEntity
    {
        public string Name { get; init; } = string.Empty;
    }

    private sealed class TestId : IEquatable<TestId>
    {
        public Guid Value { get; }
        private TestId(Guid value) => Value = value;
        public static TestId New() => new(Guid.NewGuid());
        public static TestId From(Guid value) => new(value);
        public bool Equals(TestId? other) => other is not null && Value == other.Value;
        public override bool Equals(object? obj) => obj is TestId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public static implicit operator Guid(TestId id) => id.Value;
    }

    private sealed class BaseRepositoryStub : IBaseRepository<TestEntity, TestId>
    {
        public Task<TestEntity?> GetByIdAsync(TestId id, bool includeDeleted = false, CancellationToken ct = default) =>
            Task.FromResult<TestEntity?>(null);

        public Task AddAsync(TestEntity entity, CancellationToken ct = default) =>
            Task.CompletedTask;

        public void Update(TestEntity entity)
        {
            // No-op for contract proof — stub satisfies signature
        }

        public void Delete(TestEntity entity)
        {
            // No-op for contract proof — stub satisfies signature
        }

        public Task<PagedResult<TestEntity>> GetPagedAsync(PageRequest request, bool includeDeleted = false, CancellationToken ct = default) =>
            Task.FromResult(new PagedResult<TestEntity>(
                Array.Empty<TestEntity>(), TotalCount: 0, request.Page, request.PageSize));
    }

    [Fact]
    public async Task BaseRepository_GetByIdAsync_ReturnsEntityOrNull_AndAcceptsTypedId()
    {
        IBaseRepository<TestEntity, TestId> repo = new BaseRepositoryStub();
        var id = TestId.New();

        TestEntity? result = await repo.GetByIdAsync(id, cancellationToken: CancellationToken.None);

        Assert.Null(result); // stub returns null — proves signature compiles with typed ID
    }

    [Fact]
    public async Task BaseRepository_AddAsync_AcceptsEntityAndCancellationToken()
    {
        IBaseRepository<TestEntity, TestId> repo = new BaseRepositoryStub();
        var entity = new TestEntity { Name = "test" };

        await repo.AddAsync(entity, CancellationToken.None);

        // Must not throw — proves signature compiles. Assert entity state to confirm
        // the async call completed without corrupting the passed-in reference.
        Assert.Equal("test", entity.Name);
    }

    [Fact]
    public void BaseRepository_Update_VoidMethod_AcceptsEntity()
    {
        IBaseRepository<TestEntity, TestId> repo = new BaseRepositoryStub();
        var entity = new TestEntity { Name = "test" };

        repo.Update(entity);

        // Must not throw — proves void Update signature compiles. Assert entity state
        // to confirm the void call completed without corrupting the passed-in reference.
        Assert.Equal("test", entity.Name);
    }

    [Fact]
    public void BaseRepository_Delete_VoidMethod_AcceptsEntity()
    {
        IBaseRepository<TestEntity, TestId> repo = new BaseRepositoryStub();
        var entity = new TestEntity { Name = "test" };

        repo.Delete(entity);

        // Must not throw — proves void Delete signature compiles. Assert entity state
        // to confirm the void call completed without corrupting the passed-in reference.
        Assert.Equal("test", entity.Name);
    }

    [Fact]
    public async Task BaseRepository_GetPagedAsync_ReturnsPagedResult()
    {
        IBaseRepository<TestEntity, TestId> repo = new BaseRepositoryStub();
        var request = new PageRequest(Page: 1, PageSize: 10);

        PagedResult<TestEntity> result = await repo.GetPagedAsync(request, cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task BaseRepository_GetPagedAsync_AcceptsCancellationToken()
    {
        IBaseRepository<TestEntity, TestId> repo = new BaseRepositoryStub();

        PagedResult<TestEntity> result = await repo.GetPagedAsync(
            new PageRequest(Page: 1, PageSize: 5),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
    }
}

/// <summary>
/// Minimal clock stub required by factory methods (User.Create, etc.).
/// </summary>
internal sealed class DummyClock : Project.Domain.Common.IClock
{
    public static readonly DummyClock Instance = new();

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    private DummyClock() { }
}
