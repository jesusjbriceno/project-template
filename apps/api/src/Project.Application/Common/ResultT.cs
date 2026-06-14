namespace Project.Application.Common;

/// <summary>
/// Represents the outcome of an operation that produces a value of type <typeparamref name="T"/>.
/// Use <see cref="Result"/> when the operation produces no value.
/// </summary>
/// <typeparam name="T">The type of the value produced on success.</typeparam>
public sealed class Result<T>
{
    /// <summary>
    /// True when the operation completed successfully and a value is available.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// True when the operation failed. Convenience inverse of <see cref="IsSuccess"/>.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// The value produced on success. <see langword="default"/> when <see cref="IsFailure"/> is true.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// The error details when <see cref="IsFailure"/> is true.
    /// Accessing this on a successful result returns <see langword="default"/>(<see cref="Error"/>).
    /// </summary>
    public Error Error { get; }

    private Result(bool isSuccess, T? value, Error error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    /// <summary>
    /// Creates a successful result containing the given value.
    /// </summary>
    public static Result<T> Success(T value) => new(true, value, default);

    /// <summary>
    /// Creates a failed result with a stable error code and human-readable message.
    /// </summary>
    public static Result<T> Failure(string code, string message) =>
        new(false, default, new Error(code, message));

    /// <summary>
    /// Creates a failed result from an existing <see cref="Error"/>.
    /// </summary>
    public static Result<T> Failure(Error error) => new(false, default, error);

    /// <summary>
    /// Implicitly converts a value to a successful <see cref="Result{T}"/>.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// Implicitly converts an <see cref="Error"/> to a failed <see cref="Result{T}"/>.
    /// </summary>
    public static implicit operator Result<T>(Error error) => Failure(error);
}
