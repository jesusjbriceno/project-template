namespace Project.IntegrationTests.MigrationService;

/// <summary>
/// xUnit collection definition for MigrationService integration tests.
/// All tests in this collection share a single PostgreSQL container and
/// run sequentially.
/// </summary>
[CollectionDefinition("MigrationService")]
public sealed class MigrationServiceCollection : ICollectionFixture<MigrationServiceFixture>
{
}
