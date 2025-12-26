using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalEngine.Application.Metrics.Commands;

namespace SignalEngine.SystemApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MetricsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(IMediator mediator, ILogger<MetricsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Ingests a metric value for an asset.
    /// </summary>
    [HttpPost("ingest")]
    
    [ProducesResponseType(typeof(MetricIngestionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MetricIngestionResult>> IngestMetric(
        [FromBody] IngestMetricRequest request,
        CancellationToken cancellationToken)
    {
        var command = new IngestMetricCommand
        {
            AssetId = request.AssetId,
            Name = request.Name,
            MetricTypeCode = request.MetricTypeCode,
            Value = request.Value,
            Timestamp = request.Timestamp ?? DateTime.UtcNow,
            Unit = request.Unit,
            Source = request.Source
        };

        var metricId = await _mediator.Send(command, cancellationToken);
        
        _logger.LogInformation(
            "Metric ingested: Asset={AssetId}, Name={MetricName}, Value={Value}",
            request.AssetId, request.Name, request.Value);

        return Ok(new MetricIngestionResult
        {
            Success = true,
            MetricId = metricId,
            Timestamp = command.Timestamp ?? DateTime.UtcNow
        });
    }

    /// <summary>
    /// Ingests multiple metrics in a batch.
    /// </summary>
    [HttpPost("ingest/batch")]
    
    [ProducesResponseType(typeof(BatchMetricIngestionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BatchMetricIngestionResult>> IngestMetricsBatch(
        [FromBody] BatchIngestMetricRequest request,
        CancellationToken cancellationToken)
    {
        var results = new List<MetricIngestionResult>();

        foreach (var metric in request.Metrics)
        {
            try
            {
                var command = new IngestMetricCommand
                {
                    AssetId = metric.AssetId,
                    Name = metric.Name,
                    MetricTypeCode = metric.MetricTypeCode,
                    Value = metric.Value,
                    Timestamp = metric.Timestamp ?? DateTime.UtcNow,
                    Unit = metric.Unit,
                    Source = metric.Source
                };

                var metricId = await _mediator.Send(command, cancellationToken);

                results.Add(new MetricIngestionResult
                {
                    Success = true,
                    MetricId = metricId,
                    Timestamp = command.Timestamp ?? DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ingest metric for asset {AssetId}", metric.AssetId);
                results.Add(new MetricIngestionResult
                {
                    Success = false,
                    Error = ex.Message,
                    Timestamp = metric.Timestamp ?? DateTime.UtcNow
                });
            }
        }

        return Ok(new BatchMetricIngestionResult
        {
            TotalMetrics = request.Metrics.Count,
            SuccessCount = results.Count(r => r.Success),
            FailedCount = results.Count(r => !r.Success),
            Results = results
        });
    }
}

public record IngestMetricRequest
{
    public int AssetId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string MetricTypeCode { get; init; } = string.Empty;
    public decimal Value { get; init; }
    public DateTime? Timestamp { get; init; }
    public string? Unit { get; init; }
    public string? Source { get; init; }
}

public record BatchIngestMetricRequest
{
    public List<IngestMetricRequest> Metrics { get; init; } = new();
}

public record MetricIngestionResult
{
    public bool Success { get; init; }
    public int MetricId { get; init; }
    public string? Error { get; init; }
    public DateTime Timestamp { get; init; }
}

public record BatchMetricIngestionResult
{
    public int TotalMetrics { get; init; }
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
    public List<MetricIngestionResult> Results { get; init; } = new();
}
