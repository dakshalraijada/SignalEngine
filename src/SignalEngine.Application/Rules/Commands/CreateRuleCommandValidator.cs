using FluentValidation;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Constants;

namespace SignalEngine.Application.Rules.Commands;

/// <summary>
/// Validator for CreateRuleCommand.
/// </summary>
public class CreateRuleCommandValidator : AbstractValidator<CreateRuleCommand>
{
    private readonly ILookupRepository _lookupRepository;

    public CreateRuleCommandValidator(ILookupRepository lookupRepository)
    {
        _lookupRepository = lookupRepository;

        RuleFor(x => x.AssetId)
            .GreaterThan(0).WithMessage("Asset ID must be positive.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Rule name is required.")
            .MaximumLength(100).WithMessage("Rule name cannot exceed 100 characters.");

        RuleFor(x => x.MetricName)
            .NotEmpty().WithMessage("Metric name is required.")
            .MaximumLength(100).WithMessage("Metric name cannot exceed 100 characters.");

        RuleFor(x => x.OperatorCode)
            .NotEmpty().WithMessage("Operator code is required.")
            .MustAsync(BeValidOperatorCode).WithMessage("Invalid operator code.");

        RuleFor(x => x.SeverityCode)
            .NotEmpty().WithMessage("Severity code is required.")
            .MustAsync(BeValidSeverityCode).WithMessage("Invalid severity code.");

        RuleFor(x => x.EvaluationFrequencyCode)
            .NotEmpty().WithMessage("Evaluation frequency code is required.")
            .MustAsync(BeValidEvaluationFrequencyCode).WithMessage("Invalid evaluation frequency code.");

        RuleFor(x => x.ConsecutiveBreachesRequired)
            .GreaterThan(0).WithMessage("Consecutive breaches required must be at least 1.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
    }

    private async Task<bool> BeValidOperatorCode(string code, CancellationToken cancellationToken)
    {
        var lookup = await _lookupRepository.GetLookupValueByCodeAsync(
            LookupTypeCodes.RuleOperator, code, cancellationToken);
        return lookup != null && lookup.IsActive;
    }

    private async Task<bool> BeValidSeverityCode(string code, CancellationToken cancellationToken)
    {
        var lookup = await _lookupRepository.GetLookupValueByCodeAsync(
            LookupTypeCodes.Severity, code, cancellationToken);
        return lookup != null && lookup.IsActive;
    }

    private async Task<bool> BeValidEvaluationFrequencyCode(string code, CancellationToken cancellationToken)
    {
        var lookup = await _lookupRepository.GetLookupValueByCodeAsync(
            LookupTypeCodes.RuleEvaluationFrequency, code, cancellationToken);
        return lookup != null && lookup.IsActive;
    }
}
