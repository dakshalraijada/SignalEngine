using FluentValidation;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Constants;

namespace SignalEngine.Application.Metrics.Commands;

/// <summary>
/// Validator for IngestMetricCommand.
/// </summary>
public class IngestMetricCommandValidator : AbstractValidator<IngestMetricCommand>
{
    private readonly ILookupRepository _lookupRepository;

    public IngestMetricCommandValidator(ILookupRepository lookupRepository)
    {
        _lookupRepository = lookupRepository;

        RuleFor(x => x.AssetId)
            .GreaterThan(0).WithMessage("Asset ID must be positive.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Metric name is required.")
            .MaximumLength(100).WithMessage("Metric name cannot exceed 100 characters.");

        RuleFor(x => x.MetricTypeCode)
            .NotEmpty().WithMessage("Metric type code is required.")
            .MustAsync(BeValidMetricTypeCode).WithMessage("Invalid metric type code.");

        RuleFor(x => x.Unit)
            .MaximumLength(50).WithMessage("Unit cannot exceed 50 characters.");
    }

    private async Task<bool> BeValidMetricTypeCode(string code, CancellationToken cancellationToken)
    {
        var lookup = await _lookupRepository.GetLookupValueByCodeAsync(
            LookupTypeCodes.MetricType, code, cancellationToken);
        return lookup != null && lookup.IsActive;
    }
}
