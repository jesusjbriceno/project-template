using Project.Application.Abstractions.Persistence;

namespace Project.ApplicationTests.Abstractions.Persistence;

/// <summary>
/// TDD for <see cref="PagedResult{T}"/> — a simple immutable pagination result
/// that lives in the Application layer (no Infrastructure types).
/// </summary>
public sealed class PagedResultTests
{
    [Fact]
    public void Constructor_WithItems_ExposesAllProperties()
    {
        var items = new[] { "a", "b", "c" };
        var result = new PagedResult<string>(
            items,
            TotalCount: 25,
            Page: 1,
            PageSize: 10);

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public void TotalPages_ExactDivision_ReturnsCorrectValue()
    {
        var result = new PagedResult<string>(
            Array.Empty<string>(),
            TotalCount: 30,
            Page: 1,
            PageSize: 10);

        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void TotalPages_PartialLastPage_ReturnsCeilingValue()
    {
        var result = new PagedResult<string>(
            Array.Empty<string>(),
            TotalCount: 25,
            Page: 1,
            PageSize: 10);

        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void TotalPages_ZeroTotalCount_ReturnsZero()
    {
        var result = new PagedResult<string>(
            Array.Empty<string>(),
            TotalCount: 0,
            Page: 1,
            PageSize: 10);

        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void HasNextPage_MiddlePage_ReturnsTrue()
    {
        var result = new PagedResult<string>(
            Array.Empty<string>(),
            TotalCount: 30,
            Page: 1,
            PageSize: 10);

        Assert.True(result.HasNextPage);
    }

    [Fact]
    public void HasNextPage_LastPage_ReturnsFalse()
    {
        var result = new PagedResult<string>(
            Array.Empty<string>(),
            TotalCount: 30,
            Page: 3,
            PageSize: 10);

        Assert.False(result.HasNextPage);
    }

    [Fact]
    public void HasPreviousPage_FirstPage_ReturnsFalse()
    {
        var result = new PagedResult<string>(
            Array.Empty<string>(),
            TotalCount: 30,
            Page: 1,
            PageSize: 10);

        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public void HasPreviousPage_LaterPage_ReturnsTrue()
    {
        var result = new PagedResult<string>(
            Array.Empty<string>(),
            TotalCount: 30,
            Page: 2,
            PageSize: 10);

        Assert.True(result.HasPreviousPage);
    }
}
