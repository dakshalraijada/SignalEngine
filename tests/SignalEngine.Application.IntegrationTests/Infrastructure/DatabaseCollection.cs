using Xunit;

namespace SignalEngine.Application.IntegrationTests.Infrastructure;

/// <summary>
/// Collection definition for database-dependent integration tests.
/// Tests in this collection share a single database container for performance.
/// </summary>
[CollectionDefinition(nameof(DatabaseCollection))]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
