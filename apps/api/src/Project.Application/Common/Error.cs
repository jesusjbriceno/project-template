namespace Project.Application.Common;

/// <summary>
/// Represents a stable application error with a machine-readable code
/// and a human-readable message. Used as the error payload inside
/// <see cref="Result"/> and <see cref="Result{T}"/>.
/// </summary>
public readonly record struct Error(string Code, string Message);
