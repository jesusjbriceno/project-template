using Project.Application.Common;

namespace Project.ApplicationTests.Common;

public sealed class ErrorTests
{
    [Fact]
    public void Constructor_SetsCodeAndMessage()
    {
        var error = new Error("NOT_FOUND", "The requested resource was not found");

        Assert.Equal("NOT_FOUND", error.Code);
        Assert.Equal("The requested resource was not found", error.Message);
    }

    [Fact]
    public void Equals_SameCodeAndMessage_AreEqual()
    {
        var a = new Error("CONFLICT", "Already exists");
        var b = new Error("CONFLICT", "Already exists");

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Equals_DifferentCode_AreNotEqual()
    {
        var a = new Error("NOT_FOUND", "Not found");
        var b = new Error("UNAUTHORIZED", "Not found");

        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    [Fact]
    public void Equals_DifferentMessage_AreNotEqual()
    {
        var a = new Error("FORBIDDEN", "No access");
        var b = new Error("FORBIDDEN", "Access denied");

        Assert.NotEqual(a, b);
    }

    [Theory]
    [InlineData("NOT_FOUND", "Resource missing")]
    [InlineData("UNAUTHORIZED", "Invalid credentials")]
    [InlineData("FORBIDDEN", "Insufficient permissions")]
    [InlineData("CONFLICT", "Duplicate entry")]
    [InlineData("VALIDATION_ERROR", "Input validation failed")]
    public void Constructor_AcceptsStableErrorCodes(string code, string message)
    {
        var error = new Error(code, message);

        Assert.Equal(code, error.Code);
        Assert.Equal(message, error.Message);
    }

    [Fact]
    public void DefaultError_HasNullCodeAndMessage()
    {
        var error = default(Error);

        // Default record struct: string fields are null (the default for reference types within a value type).
        Assert.Null(error.Code);
        Assert.Null(error.Message);
    }
}
