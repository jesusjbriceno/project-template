namespace Project.IntegrationTests.Infrastructure;

/// <summary>
/// xUnit collection definition for tests sharing a single PostgreSQL container.
/// All tests in this collection run sequentially against the same database instance.
/// </summary>
[CollectionDefinition("Postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
}
