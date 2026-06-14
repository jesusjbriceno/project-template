using Project.Application.Common;

namespace Project.ApplicationTests.Common;

public sealed class ResultTests
{
    [Fact]
    public void Success_IsSuccessTrue_IsFailureFalse()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void Failure_WithCodeAndMessage_IsSuccessFalse_IsFailureTrue()
    {
        var result = Result.Failure("NOT_FOUND", "The resource was not found");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Failure_WithCodeAndMessage_SetsError()
    {
        var result = Result.Failure("UNAUTHORIZED", "Invalid token");

        Assert.Equal("UNAUTHORIZED", result.Error.Code);
        Assert.Equal("Invalid token", result.Error.Message);
    }

    [Fact]
    public void Failure_FromError_SetsError()
    {
        var error = new Error("CONFLICT", "Already exists");

        var result = Result.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void ImplicitConversion_FromError_ProducesFailure()
    {
        var error = new Error("FORBIDDEN", "Not allowed");

        Result result = error;

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Generic_Success_SetsValue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Generic_Success_WithReferenceType_SetsValue()
    {
        var result = Result<string>.Success("hello");

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void Generic_Failure_WithCodeAndMessage_NoValue()
    {
        var result = Result<int>.Failure("NOT_FOUND", "Missing");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error.Code);
        Assert.Equal("Missing", result.Error.Message);
    }

    [Fact]
    public void Generic_Failure_FromError_NoValue()
    {
        var error = new Error("VALIDATION_ERROR", "Invalid input");

        var result = Result<int>.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Generic_ImplicitConversion_FromValue_ProducesSuccess()
    {
        Result<string> result = "hello world";

        Assert.True(result.IsSuccess);
        Assert.Equal("hello world", result.Value);
    }

    [Fact]
    public void Generic_ImplicitConversion_FromError_ProducesFailure()
    {
        var error = new Error("TIMEOUT", "Request timed out");

        Result<double> result = error;

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }
}
