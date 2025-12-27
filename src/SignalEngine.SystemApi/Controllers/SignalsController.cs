using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalEngine.Application.Signals.Queries;
using SignalEngine.Application.Common.DTOs;

namespace SignalEngine.SystemApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SignalsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SignalsController> _logger;

    public SignalsController(IMediator mediator, ILogger<SignalsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets signals with optional filtering.
    /// </summary>
    [HttpGet]
    
    [ProducesResponseType(typeof(IEnumerable<SignalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<SignalDto>>> GetSignals(
        [FromQuery] bool? openOnly,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSignalsQuery
        {
            OpenOnly = openOnly ?? false
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a specific signal by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    
    [ProducesResponseType(typeof(SignalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SignalDto>> GetSignal(int id, CancellationToken cancellationToken)
    {
        var query = new GetSignalsQuery();
        var results = await _mediator.Send(query, cancellationToken);
        var signal = results.FirstOrDefault(s => s.Id == id);
        
        if (signal == null)
        {
            return NotFound();
        }

        return Ok(signal);
    }

    /// <summary>
    /// Gets signal statistics.
    /// </summary>
    [HttpGet("statistics")]
    
    [ProducesResponseType(typeof(SignalStatistics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SignalStatistics>> GetStatistics(CancellationToken cancellationToken)
    {
        var query = new GetSignalsQuery();
        var signals = await _mediator.Send(query, cancellationToken);
        var signalList = signals.ToList();

        var statistics = new SignalStatistics
        {
            TotalCount = signalList.Count,
            OpenCount = signalList.Count(s => s.Resolution == null),
            ResolvedCount = signalList.Count(s => s.Resolution != null)
        };

        return Ok(statistics);
    }
}

public record SignalStatistics
{
    public int TotalCount { get; init; }
    public int OpenCount { get; init; }
    public int ResolvedCount { get; init; }
}
