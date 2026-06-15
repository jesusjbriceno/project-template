using Project.Application.Abstractions.Validation;

namespace Project.ApplicationTests.Abstractions.Validation;

/// <summary>
/// Compile-time contract proof: <see cref="IValidated"/> is a marker interface
/// that signals a command or query is eligible for FluentValidation pipeline
/// interception. Concrete validators belong in use-case slices, not here.
/// </summary>
public sealed class ValidationMarkerTests
{
    /// <summary>
    /// Sample command proving <see cref="IValidated"/> marker compiles and can be
    /// used as a type constraint for pipeline validation.
    /// </summary>
    private sealed record CreateValidatedCommand(string Name) : IValidated;

    [Fact]
    public void ValidatedCommand_IsAssignableToIValidated()
    {
        var command = new CreateValidatedCommand("test");

        // Compile-time: CreateValidatedCommand must be assignable to IValidated
        IValidated marker = command;
        Assert.NotNull(marker);
    }

    [Fact]
    public void IValidated_CanBeUsedAsGenericConstraint()
    {
        // Prove the marker can constrain a generic method — the pipeline
        // will use this pattern to selectively validate commands/queries.
        var command = new CreateValidatedCommand("example");

        bool validated = ValidateIfApplicable(command);

        Assert.True(validated);
    }

    /// <summary>
    /// Simulates the pipeline pattern: only validated types pass through.
    /// </summary>
    private static bool ValidateIfApplicable<T>(T instance) where T : IValidated
    {
        // In reality, this would resolve AbstractValidator<T> from DI.
        // For now, prove the constraint works at compile-time.
        return instance is not null;
    }
}
