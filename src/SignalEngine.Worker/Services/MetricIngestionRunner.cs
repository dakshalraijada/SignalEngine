using MediatR;
using SignalEngine.Application.Metrics.Commands;

namespace SignalEngine.Worker.Services;

/// <summary>
/// Orchestrates metric ingestion by sending the command via MediatR.
/// This service is scoped and should be resolved within a DI scope.
/// </summary>
public class MetricIngestionRunner
{
    private readonly IMediator _mediator;
    private readonly ILogger<MetricIngestionRunner> _logger;

    public MetricIngestionRunner(IMediator mediator, ILogger<MetricIngestionRunner> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Executes the metric ingestion process.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing ingestion statistics.</returns>
    public async Task<IngestMetricsResult> RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting metric ingestion cycle");

        try
        {
            var command = new IngestMetricsCommand();
            var result = await _mediator.Send(command, cancellationToken);

            if (result.AssetsProcessed > 0 || result.Errors > 0)
            {
                _logger.LogInformation(
                    "Metric ingestion completed. Assets: {Assets}, DataPoints: {DataPoints}, Errors: {Errors}, Duration: {Duration}ms",
                    result.AssetsProcessed,
                    result.DataPointsCreated,
                    result.Errors,
                    result.Duration.TotalMilliseconds);
            }
            else
            {
                _logger.LogDebug("Metric ingestion completed - no assets due");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during metric ingestion cycle");
            throw;
        }
    }
}
