namespace Project.Domain.Common;

/// <summary>
/// Abstraction over system clock for testability.
/// Domain uses DateTimeOffset exclusively.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
