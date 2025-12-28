using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Entities;

namespace SignalEngine.Application.Metrics.Commands;

/// <summary>
/// Handler for metric ingestion.
/// 
/// Ingestion Flow:
/// 1. Query all assets due for ingestion (NextIngestionAtUtc <= now OR null)
/// 2. Group assets by DataSourceId for efficient batching
/// 3. For each DataSource group:
///    a. Get the appropriate IDataSourceProvider
///    b. Call FetchBatchAsync with all asset identifiers in the group
///    c. For each asset result, fan out to create MetricData for each metric
/// 4. Update ingestion cursors (LastIngestedAtUtc, NextIngestionAtUtc)
/// 5. Save all changes in a single transaction
/// </summary>
public class IngestMetricsCommandHandler : IRequestHandler<IngestMetricsCommand, IngestMetricsResult>
{
    private readonly IIngestionRepository _ingestionRepository;
    private readonly IDataSourceProviderFactory _providerFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IngestMetricsCommandHandler> _logger;

    public IngestMetricsCommandHandler(
        IIngestionRepository ingestionRepository,
        IDataSourceProviderFactory providerFactory,
        IUnitOfWork unitOfWork,
        ILogger<IngestMetricsCommandHandler> logger)
    {
        _ingestionRepository = ingestionRepository;
        _providerFactory = providerFactory;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IngestMetricsResult> Handle(
        IngestMetricsCommand request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var assetsProcessed = 0;
        var dataPointsCreated = 0;
        var errors = 0;

        try
        {
            var nowUtc = DateTime.UtcNow;

            // Step 1: Get all assets due for ingestion
            var dueAssets = await _ingestionRepository.GetAssetsDueForIngestionAsync(nowUtc, cancellationToken);

            if (dueAssets.Count == 0)
            {
                _logger.LogDebug("No assets due for ingestion");
                return IngestMetricsResult.Empty(stopwatch.Elapsed);
            }

            _logger.LogInformation("Found {Count} assets due for ingestion", dueAssets.Count);

            // Step 2: Group by DataSource for efficient batching
            var groupedByDataSource = dueAssets
                .GroupBy(a => new { a.DataSourceId, DataSourceCode = a.DataSource?.Code ?? "UNKNOWN" })
                .ToList();

            // Step 3: Process each DataSource group
            foreach (var group in groupedByDataSource)
            {
                var dataSourceCode = group.Key.DataSourceCode;
                var assetsInGroup = group.ToList();

                _logger.LogDebug(
                    "Processing {Count} assets for DataSource {DataSource}",
                    assetsInGroup.Count,
                    dataSourceCode);

                var provider = _providerFactory.GetProvider(dataSourceCode);
                if (provider == null)
                {
                    _logger.LogWarning(
                        "No provider registered for DataSource {DataSource}, skipping {Count} assets",
                        dataSourceCode,
                        assetsInGroup.Count);
                    errors += assetsInGroup.Count;
                    continue;
                }

                // Step 3a: Batch fetch from external API
                var identifiers = assetsInGroup.Select(a => a.Identifier).ToList();
                var fetchResults = await provider.FetchBatchAsync(identifiers, cancellationToken);

                // Step 3b: Fan out results to tenant-owned MetricData
                foreach (var asset in assetsInGroup)
                {
                    try
                    {
                        if (!fetchResults.TryGetValue(asset.Identifier, out var result) || !result.Success)
                        {
                            _logger.LogWarning(
                                "Failed to fetch data for asset {AssetId} ({Identifier}): {Error}",
                                asset.Id,
                                asset.Identifier,
                                result?.ErrorMessage ?? "No result");
                            errors++;
                            continue;
                        }

                        // Create MetricData for each fetched value that matches a metric
                        var createdCount = await CreateMetricDataAsync(asset, result.Values, nowUtc, cancellationToken);
                        dataPointsCreated += createdCount;

                        // Step 4: Update ingestion cursor
                        await _ingestionRepository.UpdateIngestionCursorAsync(
                            asset.Id,
                            nowUtc,
                            nowUtc.AddSeconds(asset.IngestionIntervalSeconds),
                            cancellationToken);

                        assetsProcessed++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error processing asset {AssetId} ({Identifier})",
                            asset.Id,
                            asset.Identifier);
                        errors++;
                    }
                }
            }

            // Step 5: Save all changes in single transaction
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "Ingestion completed. Assets: {Assets}, DataPoints: {DataPoints}, Errors: {Errors}, Duration: {Duration}ms",
                assetsProcessed,
                dataPointsCreated,
                errors,
                stopwatch.ElapsedMilliseconds);

            return new IngestMetricsResult(assetsProcessed, dataPointsCreated, errors, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during metric ingestion");
            throw;
        }
    }

    /// <summary>
    /// Creates MetricData entries for fetched values that match defined metrics.
    /// </summary>
    private async Task<int> CreateMetricDataAsync(
        Asset asset,
        IReadOnlyList<FetchedMetricValue> fetchedValues,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        // Asset should have Metrics eagerly loaded
        var assetMetrics = asset.Metrics;
        if (assetMetrics.Count == 0)
        {
            _logger.LogDebug(
                "Asset {AssetId} has no active metrics defined, skipping data creation",
                asset.Id);
            return 0;
        }

        var dataPoints = new List<MetricData>();

        foreach (var fetched in fetchedValues)
        {
            // Find matching metric by name (case-insensitive)
            var metric = assetMetrics.FirstOrDefault(m =>
                string.Equals(m.Name, fetched.MetricName, StringComparison.OrdinalIgnoreCase));

            if (metric == null)
            {
                // No metric defined for this fetched value - skip
                _logger.LogTrace(
                    "No metric '{MetricName}' defined for asset {AssetId}, skipping",
                    fetched.MetricName,
                    asset.Id);
                continue;
            }

            // Create tenant-scoped MetricData
            var metricData = new MetricData(
                tenantId: asset.TenantId,
                metricId: metric.Id,
                value: fetched.Value,
                timestamp: fetched.Timestamp);

            dataPoints.Add(metricData);
        }

        if (dataPoints.Count > 0)
        {
            await _ingestionRepository.AddMetricDataBatchAsync(dataPoints, cancellationToken);
        }

        return dataPoints.Count;
    }
}
