namespace Infrastructure.IntegrationTests;

[CollectionDefinition("PostgresCollection")]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture>;

