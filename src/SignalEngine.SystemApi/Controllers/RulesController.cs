using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalEngine.Application.Rules.Commands;
using SignalEngine.Application.Rules.Queries;
using SignalEngine.Application.Common.DTOs;

namespace SignalEngine.SystemApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RulesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<RulesController> _logger;

    public RulesController(IMediator mediator, ILogger<RulesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets all rules for the current tenant.
    /// </summary>
    [HttpGet]
    
    [ProducesResponseType(typeof(IEnumerable<RuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<RuleDto>>> GetRules(
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken)
    {
        var query = new GetRulesQuery
        {
            ActiveOnly = activeOnly ?? false
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new rule.
    /// </summary>
    [HttpPost]
    
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<int>> CreateRule(
        [FromBody] CreateRuleRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateRuleCommand
        {
            AssetId = request.AssetId,
            Name = request.Name,
            Description = request.Description,
            MetricName = request.MetricName,
            OperatorCode = request.OperatorCode,
            Threshold = request.Threshold,
            SeverityCode = request.SeverityCode,
            EvaluationFrequencyCode = request.EvaluationFrequencyCode,
            ConsecutiveBreachesRequired = request.ConsecutiveBreachesRequired
        };

        var ruleId = await _mediator.Send(command, cancellationToken);
        
        _logger.LogInformation("Rule {RuleId} created", ruleId);
        
        return CreatedAtAction(nameof(GetRules), new { id = ruleId }, ruleId);
    }

    /// <summary>
    /// Disables a rule.
    /// </summary>
    [HttpPut("{id:int}/disable")]
    
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DisableRule(int id, CancellationToken cancellationToken)
    {
        var command = new DisableRuleCommand(id);

        await _mediator.Send(command, cancellationToken);
        
        _logger.LogInformation("Rule {RuleId} disabled", id);
        
        return NoContent();
    }
}

public record CreateRuleRequest
{
    public int AssetId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string MetricName { get; init; } = string.Empty;
    public string OperatorCode { get; init; } = string.Empty;
    public decimal Threshold { get; init; }
    public string SeverityCode { get; init; } = string.Empty;
    public string EvaluationFrequencyCode { get; init; } = string.Empty;
    public int ConsecutiveBreachesRequired { get; init; } = 1;
}
