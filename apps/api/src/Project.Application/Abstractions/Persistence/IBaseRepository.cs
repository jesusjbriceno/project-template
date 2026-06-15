namespace Project.Application.Abstractions.Persistence;

/// <summary>
/// Shared repository contract exposing common CRUD primitives and paginated search.
/// Concrete per-aggregate repositories inherit from this base and add aggregate-specific queries.
///
/// <typeparam name="TEntity">Domain entity type.</typeparam>
/// <typeparam name="TId">Strongly-typed entity identifier (e.g. UserId, RoleId).</typeparam>
///
/// Does NOT reference EF Core, DbContext, IQueryable, ASP.NET, or Infrastructure types.
/// </summary>
public interface IBaseRepository<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    /// <summary>
    /// Retrieves an entity by its strongly-typed identifier, or null if not found.
    /// </summary>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new entity.
    /// </summary>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an existing entity as modified. Must be followed by a unit-of-work save.
    /// </summary>
    void Update(TEntity entity);

    /// <summary>
    /// Marks an entity for deletion. Must be followed by a unit-of-work save.
    /// </summary>
    void Delete(TEntity entity);

    /// <summary>
    /// Returns a paginated subset of entities without exposing IQueryable or provider-specific constructs.
    /// Infrastructure implementations handle the actual paging (EF Core Skip/Take, Dapper OFFSET/FETCH).
    /// </summary>
    Task<PagedResult<TEntity>> GetPagedAsync(PageRequest request, CancellationToken cancellationToken = default);
}
