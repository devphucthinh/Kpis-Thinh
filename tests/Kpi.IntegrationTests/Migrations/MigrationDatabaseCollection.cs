using Xunit;

namespace Kpi.IntegrationTests.Migrations;

[CollectionDefinition("PostgreSQL migration contract", DisableParallelization = true)]
public sealed class MigrationDatabaseCollection : ICollectionFixture<MigrationDatabaseFixture>;
