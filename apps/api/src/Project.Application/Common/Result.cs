namespace Project.Application.Common;

/// <summary>
/// Represents the outcome of an operation that returns no value.
/// Use <see cref="Result{T}"/> when the operation produces a value.
/// </summary>
public sealed class Result
{
    /// <summary>
    /// True when the operation completed successfully.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// True when the operation failed. Convenience inverse of <see cref="IsSuccess"/>.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// The error details when <see cref="IsFailure"/> is true.
    /// Accessing this on a successful result returns <see langword="default"/>(<see cref="Error"/>).
    /// </summary>
    public Error Error { get; }

    private Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Creates a successful result with no value.
    /// </summary>
    public static Result Success() => new(true, default);

    /// <summary>
    /// Creates a failed result with a stable error code and human-readable message.
    /// </summary>
    public static Result Failure(string code, string message) =>
        new(false, new Error(code, message));

    /// <summary>
    /// Creates a failed result from an existing <see cref="Error"/>.
    /// </summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Implicitly converts an <see cref="Error"/> to a failed <see cref="Result"/>.
    /// </summary>
    public static implicit operator Result(Error error) => Failure(error);
}
