using Project.Domain.Common;

namespace Project.UnitTests.Common;

public class AuditableEntityTests
{
    private sealed class TestEntity : AuditableEntity
    {
        public string Name { get; }

        public TestEntity(string name)
        {
            Name = name;
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    [Fact]
    public void MarkCreated_SetsCreatedAndUpdatedTimestamps()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 6, 12, 10, 0, 0, TimeSpan.Zero));
        var entity = new TestEntity("test");

        entity.MarkCreated("creator-1", clock);

        Assert.Equal(new DateTimeOffset(2026, 6, 12, 10, 0, 0, TimeSpan.Zero), entity.CreatedAt);
        Assert.Equal("creator-1", entity.CreatedBy);
        Assert.Equal(new DateTimeOffset(2026, 6, 12, 10, 0, 0, TimeSpan.Zero), entity.UpdatedAt);
        Assert.Equal("creator-1", entity.UpdatedBy);
    }

    [Fact]
    public void MarkUpdated_OnlyModifiesUpdatedFields()
    {
        var createClock = new FixedClock(new DateTimeOffset(2026, 6, 12, 10, 0, 0, TimeSpan.Zero));
        var updateClock = new FixedClock(new DateTimeOffset(2026, 6, 13, 14, 30, 0, TimeSpan.Zero));
        var entity = new TestEntity("test");

        entity.MarkCreated("creator-1", createClock);
        entity.MarkUpdated("updater-1", updateClock);

        // Created fields unchanged
        Assert.Equal(new DateTimeOffset(2026, 6, 12, 10, 0, 0, TimeSpan.Zero), entity.CreatedAt);
        Assert.Equal("creator-1", entity.CreatedBy);

        // Updated fields reflect the update
        Assert.Equal(new DateTimeOffset(2026, 6, 13, 14, 30, 0, TimeSpan.Zero), entity.UpdatedAt);
        Assert.Equal("updater-1", entity.UpdatedBy);
    }

    [Fact]
    public void MarkDeleted_SetsDeletionFields()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 6, 14, 9, 0, 0, TimeSpan.Zero));
        var entity = new TestEntity("test");

        entity.MarkCreated("creator-1", clock);
        entity.MarkDeleted("deleter-1", new FixedClock(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero)));

        Assert.Equal(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero), entity.DeletedAt);
        Assert.Equal("deleter-1", entity.DeletedBy);
        Assert.True(entity.IsDeleted);
    }

    [Fact]
    public void NewEntity_IsNotDeleted()
    {
        var entity = new TestEntity("test");

        Assert.False(entity.IsDeleted);
        Assert.Null(entity.DeletedAt);
        Assert.Null(entity.DeletedBy);
    }

    [Fact]
    public void NewEntity_HasDefaultTimestamps()
    {
        var entity = new TestEntity("test");

        Assert.Equal(default, entity.CreatedAt);
        Assert.Null(entity.CreatedBy);
        Assert.Equal(default, entity.UpdatedAt);
        Assert.Null(entity.UpdatedBy);
    }

    [Fact]
    public void UseDateTimeOffset_NotDateTime()
    {
        var entity = new TestEntity("test");

        // Verify the types are DateTimeOffset, not DateTime
        Assert.IsType<DateTimeOffset>(entity.CreatedAt);
        Assert.IsType<DateTimeOffset>(entity.UpdatedAt);

        // DeletedAt is nullable DateTimeOffset
        var deletedAtType = typeof(AuditableEntity).GetProperty(nameof(AuditableEntity.DeletedAt))!.PropertyType;
        Assert.Equal(typeof(DateTimeOffset?), deletedAtType);
    }
}
