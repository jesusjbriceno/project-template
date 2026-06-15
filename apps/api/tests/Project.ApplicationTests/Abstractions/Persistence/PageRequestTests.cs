using Project.Application.Abstractions.Persistence;

namespace Project.ApplicationTests.Abstractions.Persistence;

/// <summary>
/// TDD for <see cref="PageRequest"/> — a simple immutable pagination descriptor
/// that lives in the Application layer (no EF Core, no ASP.NET references).
/// </summary>
public sealed class PageRequestTests
{
    [Fact]
    public void Constructor_ValidPageAndPageSize_CreatesInstance()
    {
        var request = new PageRequest(Page: 1, PageSize: 10);

        Assert.Equal(1, request.Page);
        Assert.Equal(10, request.PageSize);
    }

    [Fact]
    public void Constructor_PageZero_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new PageRequest(Page: 0, PageSize: 10));

        Assert.Contains("page", ex.ParamName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_PageNegative_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new PageRequest(Page: -1, PageSize: 10));

        Assert.Contains("page", ex.ParamName, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_PageSizeZeroOrNegative_ThrowsArgumentOutOfRangeException(int pageSize)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new PageRequest(Page: 1, PageSize: pageSize));

        Assert.Contains("pageSize", ex.ParamName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_PageSizeExceedsMax_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new PageRequest(Page: 1, PageSize: PageRequest.MaxPageSize + 1));

        Assert.Contains("pageSize", ex.ParamName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_PageSizeAtMax_CreatesInstance()
    {
        var request = new PageRequest(Page: 5, PageSize: PageRequest.MaxPageSize);

        Assert.Equal(5, request.Page);
        Assert.Equal(PageRequest.MaxPageSize, request.PageSize);
    }

    [Fact]
    public void MaxPageSize_IsReasonableLimit()
    {
        Assert.True(PageRequest.MaxPageSize > 0);
        Assert.True(PageRequest.MaxPageSize <= 200);
    }
}
