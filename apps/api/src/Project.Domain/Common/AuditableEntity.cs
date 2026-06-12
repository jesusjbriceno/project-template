namespace Project.Domain.Common;

/// <summary>
/// Base class for domain entities requiring audit tracking and soft-delete support.
/// Uses DateTimeOffset exclusively (never DateTime).
/// </summary>
public abstract class AuditableEntity
{
    public DateTimeOffset CreatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public string? DeletedBy { get; private set; }

    public bool IsDeleted => DeletedAt.HasValue;

    /// <summary>
    /// Marks the entity as newly created, setting both Created and Updated fields.
    /// </summary>
    public void MarkCreated(string createdBy, IClock clock)
    {
        var now = clock.UtcNow;
        CreatedAt = now;
        CreatedBy = createdBy;
        UpdatedAt = now;
        UpdatedBy = createdBy;
    }

    /// <summary>
    /// Marks the entity as updated, updating the Updated fields.
    /// </summary>
    public void MarkUpdated(string updatedBy, IClock clock)
    {
        UpdatedAt = clock.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Marks the entity as soft-deleted, setting the Deleted fields.
    /// Protected so subclasses can override to enforce deletion guards (e.g. system role protection)
    /// without external callers bypassing entity-specific rules via an AuditableEntity reference.
    /// </summary>
    protected virtual void MarkDeleted(string deletedBy, IClock clock)
    {
        DeletedAt = clock.UtcNow;
        DeletedBy = deletedBy;
    }
}
