namespace SignalEngine.Domain.Constants;

/// <summary>
/// Defines lookup type codes used throughout the system.
/// These codes map to LookupType.Code in the database.
/// </summary>
public static class LookupTypeCodes
{
    public const string TenantType = "TENANT_TYPE";
    public const string PlanCode = "PLAN_CODE";
    public const string RuleOperator = "RULE_OPERATOR";
    public const string Severity = "SEVERITY";
    public const string AssetType = "ASSET_TYPE";
    public const string MetricType = "METRIC_TYPE";
    public const string SignalStatus = "SIGNAL_STATUS";
    public const string NotificationChannelType = "NOTIFICATION_CHANNEL_TYPE";
    public const string RuleEvaluationFrequency = "RULE_EVALUATION_FREQUENCY";
    public const string DataSource = "DATA_SOURCE";
}

/// <summary>
/// Defines lookup value codes for DATA_SOURCE.
/// Used by Asset.DataSourceId to indicate where data originates.
/// </summary>
public static class DataSourceCodes
{
    public const string Binance = "BINANCE";
    public const string Coinbase = "COINBASE";
    public const string Kraken = "KRAKEN";
    public const string CustomApi = "CUSTOM_API";
}

/// <summary>
/// Defines lookup value codes for TENANT_TYPE.
/// </summary>
public static class TenantTypeCodes
{
    public const string B2C = "B2C";
    public const string B2B = "B2B";
}

/// <summary>
/// Defines lookup value codes for PLAN_CODE.
/// </summary>
public static class PlanCodes
{
    public const string Free = "FREE";
    public const string Pro = "PRO";
    public const string Business = "BUSINESS";
}

/// <summary>
/// Defines lookup value codes for RULE_OPERATOR.
/// </summary>
public static class RuleOperatorCodes
{
    public const string GreaterThan = "GT";
    public const string LessThan = "LT";
    public const string Equal = "EQ";
    public const string GreaterThanOrEqual = "GTE";
    public const string LessThanOrEqual = "LTE";
}

/// <summary>
/// Defines lookup value codes for SEVERITY.
/// </summary>
public static class SeverityCodes
{
    public const string Info = "INFO";
    public const string Warning = "WARNING";
    public const string Critical = "CRITICAL";
}

/// <summary>
/// Defines lookup value codes for ASSET_TYPE.
/// </summary>
public static class AssetTypeCodes
{
    public const string Crypto = "CRYPTO";
    public const string Website = "WEBSITE";
    public const string Service = "SERVICE";
}

/// <summary>
/// Defines lookup value codes for METRIC_TYPE.
/// </summary>
public static class MetricTypeCodes
{
    public const string Numeric = "NUMERIC";
    public const string Percentage = "PERCENTAGE";
    public const string Rate = "RATE";
}

/// <summary>
/// Defines lookup value codes for SIGNAL_STATUS.
/// </summary>
public static class SignalStatusCodes
{
    public const string Open = "OPEN";
    public const string Resolved = "RESOLVED";
}

/// <summary>
/// Defines lookup value codes for NOTIFICATION_CHANNEL_TYPE.
/// </summary>
public static class NotificationChannelTypeCodes
{
    public const string Email = "EMAIL";
    public const string Webhook = "WEBHOOK";
    public const string Slack = "SLACK";
}

/// <summary>
/// Defines lookup value codes for RULE_EVALUATION_FREQUENCY.
/// </summary>
public static class RuleEvaluationFrequencyCodes
{
    public const string OneMinute = "1_MIN";
    public const string FiveMinutes = "5_MIN";
    public const string FifteenMinutes = "15_MIN";
}
