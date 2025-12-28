using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalEngine.Application.Common.Interfaces;

namespace SignalEngine.SystemApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssetsController : ControllerBase
{
    private readonly IAssetRepository _assetRepository;
    private readonly ILogger<AssetsController> _logger;

    public AssetsController(IAssetRepository assetRepository, ILogger<AssetsController> logger)
    {
        _assetRepository = assetRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets all assets for the current tenant.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AssetResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<AssetResponse>>> GetAssets(CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        if (!tenantId.HasValue)
        {
            return Forbid();
        }

        var assets = await _assetRepository.GetByTenantIdAsync(tenantId.Value, cancellationToken);
        
        var response = assets.Select(a => new AssetResponse
        {
            Id = a.Id,
            TenantId = a.TenantId,
            Name = a.Name,
            Identifier = a.Identifier,
            AssetTypeId = a.AssetTypeId,
            DataSourceId = a.DataSourceId,
            Description = a.Description,
            Metadata = a.Metadata,
            IsActive = a.IsActive,
            CreatedAt = a.CreatedAt
        });

        return Ok(response);
    }

    /// <summary>
    /// Gets a specific asset by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AssetResponse>> GetAsset(int id, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        if (!tenantId.HasValue)
        {
            return Forbid();
        }

        var asset = await _assetRepository.GetByIdAsync(id, cancellationToken);
        
        if (asset == null || asset.TenantId != tenantId.Value)
        {
            return NotFound();
        }

        var response = new AssetResponse
        {
            Id = asset.Id,
            TenantId = asset.TenantId,
            Name = asset.Name,
            Identifier = asset.Identifier,
            AssetTypeId = asset.AssetTypeId,
            DataSourceId = asset.DataSourceId,
            Description = asset.Description,
            Metadata = asset.Metadata,
            IsActive = asset.IsActive,
            CreatedAt = asset.CreatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Creates a new asset.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AssetResponse>> CreateAsset(
        [FromBody] CreateAssetRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        if (!tenantId.HasValue)
        {
            return Forbid();
        }

        var asset = new Domain.Entities.Asset(
            tenantId: tenantId.Value,
            name: request.Name,
            identifier: request.Identifier,
            assetTypeId: request.AssetTypeId,
            dataSourceId: request.DataSourceId,
            description: request.Description,
            metadata: request.Metadata
        );

        await _assetRepository.AddAsync(asset, cancellationToken);

        _logger.LogInformation("Asset {AssetId} created for tenant {TenantId}", asset.Id, tenantId);

        var response = new AssetResponse
        {
            Id = asset.Id,
            TenantId = asset.TenantId,
            Name = asset.Name,
            Identifier = asset.Identifier,
            AssetTypeId = asset.AssetTypeId,
            DataSourceId = asset.DataSourceId,
            Description = asset.Description,
            Metadata = asset.Metadata,
            IsActive = asset.IsActive,
            CreatedAt = asset.CreatedAt
        };

        return CreatedAtAction(nameof(GetAsset), new { id = asset.Id }, response);
    }

    /// <summary>
    /// Updates an existing asset.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AssetResponse>> UpdateAsset(
        int id,
        [FromBody] UpdateAssetRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        if (!tenantId.HasValue)
        {
            return Forbid();
        }

        var asset = await _assetRepository.GetByIdAsync(id, cancellationToken);
        
        if (asset == null || asset.TenantId != tenantId.Value)
        {
            return NotFound();
        }

        asset.Update(request.Name, request.Identifier, request.Description, request.Metadata);

        await _assetRepository.UpdateAsync(asset, cancellationToken);

        _logger.LogInformation("Asset {AssetId} updated for tenant {TenantId}", id, tenantId);

        var response = new AssetResponse
        {
            Id = asset.Id,
            TenantId = asset.TenantId,
            Name = asset.Name,
            Identifier = asset.Identifier,
            AssetTypeId = asset.AssetTypeId,
            DataSourceId = asset.DataSourceId,
            Description = asset.Description,
            Metadata = asset.Metadata,
            IsActive = asset.IsActive,
            CreatedAt = asset.CreatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Activates an asset.
    /// </summary>
    [HttpPut("{id:int}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ActivateAsset(int id, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        if (!tenantId.HasValue)
        {
            return Forbid();
        }

        var asset = await _assetRepository.GetByIdAsync(id, cancellationToken);
        
        if (asset == null || asset.TenantId != tenantId.Value)
        {
            return NotFound();
        }

        asset.Activate();
        await _assetRepository.UpdateAsync(asset, cancellationToken);

        _logger.LogInformation("Asset {AssetId} activated for tenant {TenantId}", id, tenantId);

        return NoContent();
    }

    /// <summary>
    /// Deactivates an asset.
    /// </summary>
    [HttpPut("{id:int}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeactivateAsset(int id, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        if (!tenantId.HasValue)
        {
            return Forbid();
        }

        var asset = await _assetRepository.GetByIdAsync(id, cancellationToken);
        
        if (asset == null || asset.TenantId != tenantId.Value)
        {
            return NotFound();
        }

        asset.Deactivate();
        await _assetRepository.UpdateAsync(asset, cancellationToken);

        _logger.LogInformation("Asset {AssetId} deactivated for tenant {TenantId}", id, tenantId);

        return NoContent();
    }

    /// <summary>
    /// Deletes an asset.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAsset(int id, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        if (!tenantId.HasValue)
        {
            return Forbid();
        }

        var asset = await _assetRepository.GetByIdAsync(id, cancellationToken);
        
        if (asset == null || asset.TenantId != tenantId.Value)
        {
            return NotFound();
        }

        await _assetRepository.DeleteAsync(asset, cancellationToken);

        _logger.LogInformation("Asset {AssetId} deleted for tenant {TenantId}", id, tenantId);

        return NoContent();
    }

    private int? GetTenantId()
    {
        var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
        return int.TryParse(tenantIdClaim, out var tenantId) ? tenantId : null;
    }
}

public record AssetResponse
{
    public int Id { get; init; }
    public int TenantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Identifier { get; init; } = string.Empty;
    public int AssetTypeId { get; init; }
    public int DataSourceId { get; init; }
    public string? Description { get; init; }
    public string? Metadata { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateAssetRequest
{
    public string Name { get; init; } = string.Empty;
    public string Identifier { get; init; } = string.Empty;
    public int AssetTypeId { get; init; }
    public int DataSourceId { get; init; }
    public string? Description { get; init; }
    public string? Metadata { get; init; }
}

public record UpdateAssetRequest
{
    public string Name { get; init; } = string.Empty;
    public string Identifier { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Metadata { get; init; }
}
