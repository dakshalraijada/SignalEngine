using MediatR;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Constants;
using SignalEngine.Domain.Entities;
using SignalEngine.Domain.Exceptions;

namespace SignalEngine.Application.Metrics.Commands;

/// <summary>
/// Handler for IngestMetricCommand.
/// </summary>
public class IngestMetricCommandHandler : IRequestHandler<IngestMetricCommand, int>
{
    private readonly IMetricRepository _metricRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly ILookupRepository _lookupRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public IngestMetricCommandHandler(
        IMetricRepository metricRepository,
        IAssetRepository assetRepository,
        ILookupRepository lookupRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _metricRepository = metricRepository;
        _assetRepository = assetRepository;
        _lookupRepository = lookupRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(IngestMetricCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId
            ?? throw new InvalidOperationException("User must be associated with a tenant.");

        // Validate asset belongs to tenant
        var asset = await _assetRepository.GetByIdAsync(request.AssetId, cancellationToken)
            ?? throw new EntityNotFoundException("Asset", request.AssetId);

        if (asset.TenantId != tenantId)
            throw new TenantAccessDeniedException(tenantId, asset.TenantId);

        // Resolve metric type
        var metricTypeId = await _lookupRepository.ResolveLookupIdAsync(
            LookupTypeCodes.MetricType, request.MetricTypeCode, cancellationToken);

        // Create metric
        var metric = new Metric(
            tenantId,
            request.AssetId,
            request.Name,
            metricTypeId,
            request.Value,
            request.Timestamp ?? DateTime.UtcNow,
            request.Unit,
            request.Source);

        await _metricRepository.AddAsync(metric, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return metric.Id;
    }
}
