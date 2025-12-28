namespace SignalEngine.Application.Common.Interfaces;

/// <summary>
/// Factory for resolving data source providers by data source code.
/// </summary>
public interface IDataSourceProviderFactory
{
    /// <summary>
    /// Gets the data source provider for the given data source code.
    /// </summary>
    /// <param name="dataSourceCode">The data source code (e.g., "BINANCE", "COINBASE").</param>
    /// <returns>The provider, or null if no provider exists for this code.</returns>
    IDataSourceProvider? GetProvider(string dataSourceCode);

    /// <summary>
    /// Gets all registered data source codes.
    /// </summary>
    IReadOnlyList<string> GetRegisteredDataSourceCodes();
}
