using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Project.Application.Abstractions.Persistence;

namespace Project.Infrastructure.Data.Repositories;

/// <summary>
/// Generic base repository providing CRUD primitives and paginated search over EF Core.
/// Uses <see cref="QueryTrackingBehavior.NoTracking"/> by default (set globally on
/// <see cref="ApplicationDbContext"/>). Obeys the <c>includeDeleted</c> flag for entities
/// with soft-delete query filters by calling <see cref="EntityFrameworkQueryableExtensions.IgnoreQueryFilters{T}"/>
/// when the flag is true.
///
/// <typeparam name="TEntity">Domain entity type. Must have an accessible <c>Id</c> property.</typeparam>
/// <typeparam name="TId">Strongly-typed entity identifier (e.g. UserId, RoleId).</typeparam>
/// </summary>
public class BaseRepository<TEntity, TId> : IBaseRepository<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    protected readonly ApplicationDbContext DbContext;
    protected DbSet<TEntity> Set => DbContext.Set<TEntity>();

    public BaseRepository(ApplicationDbContext dbContext)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc />
    public virtual async Task<TEntity?> GetByIdAsync(
        TId id, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = Set.AsQueryable();
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        return await query.FirstOrDefaultAsync(BuildIdPredicate(id), cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await Set.AddAsync(entity, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Update(TEntity entity)
    {
        Set.Update(entity);
    }

    /// <inheritdoc />
    public void Delete(TEntity entity)
    {
        Set.Remove(entity);
    }

    /// <inheritdoc />
    public virtual async Task<PagedResult<TEntity>> GetPagedAsync(
        PageRequest request, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = Set.AsQueryable();
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(BuildIdSelector())
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TEntity>(items, totalCount, request.Page, request.PageSize);
    }

    /// <summary>
    /// Builds an expression tree for <c>e => e.Id == id</c> so that strongly-typed IDs
    /// are compared without requiring a common interface constraint on <typeparamref name="TEntity"/>.
    /// </summary>
    private static Expression<Func<TEntity, bool>> BuildIdPredicate(TId id)
    {
        var param = Expression.Parameter(typeof(TEntity), "e");
        var property = Expression.Property(param, "Id");
        var constant = Expression.Constant(id, typeof(TId));
        var body = Expression.Equal(property, constant);
        return Expression.Lambda<Func<TEntity, bool>>(body, param);
    }

    /// <summary>
    /// Builds an expression tree for <c>e => (object)e.Id</c> as a stable default ordering key.
    /// EF Core strips the boxing cast during SQL generation, producing <c>ORDER BY "Id"</c>.
    /// </summary>
    private static Expression<Func<TEntity, object>> BuildIdSelector()
    {
        var param = Expression.Parameter(typeof(TEntity), "e");
        var idProp = Expression.Property(param, "Id");
        var convert = Expression.Convert(idProp, typeof(object));
        return Expression.Lambda<Func<TEntity, object>>(convert, param);
    }
}
