using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalEngine.Application.Common.Interfaces;

namespace SignalEngine.SystemApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LookupsController : ControllerBase
{
    private readonly ILookupRepository _lookupRepository;
    private readonly ILogger<LookupsController> _logger;

    public LookupsController(ILookupRepository lookupRepository, ILogger<LookupsController> logger)
    {
        _lookupRepository = lookupRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets all lookup types.
    /// </summary>
    [HttpGet("types")]
    [ProducesResponseType(typeof(IEnumerable<LookupTypeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<LookupTypeResponse>>> GetLookupTypes(CancellationToken cancellationToken)
    {
        var lookupTypes = await _lookupRepository.GetAllLookupTypesAsync(cancellationToken);
        
        var response = lookupTypes.Select(lt => new LookupTypeResponse
        {
            Id = lt.Id,
            Code = lt.Code,
            Description = lt.Description
        });

        return Ok(response);
    }

    /// <summary>
    /// Gets lookup values by type code.
    /// </summary>
    [HttpGet("values/{typeCode}")]
    [ProducesResponseType(typeof(IEnumerable<LookupValueResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<LookupValueResponse>>> GetLookupValues(
        string typeCode,
        CancellationToken cancellationToken)
    {
        var lookupValues = await _lookupRepository.GetLookupValuesByTypeCodeAsync(typeCode, cancellationToken);
        
        if (!lookupValues.Any())
        {
            return NotFound($"Lookup type '{typeCode}' not found or has no values.");
        }

        var response = lookupValues.Select(lv => new LookupValueResponse
        {
            Id = lv.Id,
            Code = lv.Code,
            Name = lv.Name,
            SortOrder = lv.SortOrder,
            IsActive = lv.IsActive
        });

        return Ok(response);
    }

    /// <summary>
    /// Gets a specific lookup value by ID.
    /// </summary>
    [HttpGet("values/by-id/{id:int}")]
    [ProducesResponseType(typeof(LookupValueResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LookupValueResponse>> GetLookupValue(
        int id,
        CancellationToken cancellationToken)
    {
        var lookupValue = await _lookupRepository.GetLookupValueByIdAsync(id, cancellationToken);
        
        if (lookupValue == null)
        {
            return NotFound();
        }

        var response = new LookupValueResponse
        {
            Id = lookupValue.Id,
            Code = lookupValue.Code,
            Name = lookupValue.Name,
            SortOrder = lookupValue.SortOrder,
            IsActive = lookupValue.IsActive
        };

        return Ok(response);
    }

    /// <summary>
    /// Gets all active severity lookup values.
    /// </summary>
    [HttpGet("severities")]
    [ProducesResponseType(typeof(IEnumerable<LookupValueResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<LookupValueResponse>>> GetSeverities(CancellationToken cancellationToken)
    {
        var severities = await _lookupRepository.GetLookupValuesByTypeCodeAsync(
            Domain.Constants.LookupTypeCodes.Severity, 
            cancellationToken);

        var response = severities
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .Select(lv => new LookupValueResponse
            {
                Id = lv.Id,
                Code = lv.Code,
                Name = lv.Name,
                SortOrder = lv.SortOrder,
                IsActive = lv.IsActive
            });

        return Ok(response);
    }

    /// <summary>
    /// Gets all active asset type lookup values.
    /// </summary>
    [HttpGet("asset-types")]
    [ProducesResponseType(typeof(IEnumerable<LookupValueResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<LookupValueResponse>>> GetAssetTypes(
        CancellationToken cancellationToken)
    {
        var types = await _lookupRepository.GetLookupValuesByTypeCodeAsync(
            Domain.Constants.LookupTypeCodes.AssetType, 
            cancellationToken);

        var response = types
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder)
            .Select(lv => new LookupValueResponse
            {
                Id = lv.Id,
                Code = lv.Code,
                Name = lv.Name,
                SortOrder = lv.SortOrder,
                IsActive = lv.IsActive
            });

        return Ok(response);
    }

    /// <summary>
    /// Gets all active rule operator lookup values.
    /// </summary>
    [HttpGet("operators")]
    [ProducesResponseType(typeof(IEnumerable<LookupValueResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<LookupValueResponse>>> GetOperators(
        CancellationToken cancellationToken)
    {
        var operators = await _lookupRepository.GetLookupValuesByTypeCodeAsync(
            Domain.Constants.LookupTypeCodes.RuleOperator, 
            cancellationToken);

        var response = operators
            .Where(o => o.IsActive)
            .OrderBy(o => o.SortOrder)
            .Select(lv => new LookupValueResponse
            {
                Id = lv.Id,
                Code = lv.Code,
                Name = lv.Name,
                SortOrder = lv.SortOrder,
                IsActive = lv.IsActive
            });

        return Ok(response);
    }
}

public record LookupTypeResponse
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public record LookupValueResponse
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
}
