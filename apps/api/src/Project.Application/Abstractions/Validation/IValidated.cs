namespace Project.Application.Abstractions.Validation;

/// <summary>
/// Marker interface signalling that a command or query is eligible for
/// FluentValidation pipeline interception.
/// </summary>
/// <remarks>
/// Concrete <c>AbstractValidator&lt;T&gt;</c> implementations for specific
/// commands and queries belong in their respective use-case slices, not in
/// the foundation layer. This interface exists only to enable the validation
/// pipeline to identify types that carry validators at runtime.
/// </remarks>
public interface IValidated
{
}
