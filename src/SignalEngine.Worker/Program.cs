using SignalEngine.Application;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Infrastructure;
using SignalEngine.Worker.Options;
using SignalEngine.Worker.Services;
using SignalEngine.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Configure services from Application and Infrastructure layers
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration, includeDataSeeder: false);

// Register system-level current user service for background workers
// This returns null TenantId, which disables tenant filtering for system operations
builder.Services.AddScoped<ICurrentUserService, SystemCurrentUserService>();

// Configure rule evaluation options
builder.Services.Configure<RuleEvaluationOptions>(
    builder.Configuration.GetSection("RuleEvaluation"));

// Configure metric ingestion options
builder.Services.Configure<MetricIngestionOptions>(
    builder.Configuration.GetSection("MetricIngestion"));

// Configure notification dispatch options
builder.Services.Configure<NotificationOptions>(
    builder.Configuration.GetSection("Notification"));

// Register the rule evaluation runner as scoped
// (created fresh for each evaluation cycle)
builder.Services.AddScoped<RuleEvaluationRunner>();

// Register the metric ingestion runner as scoped
// (created fresh for each ingestion cycle)
builder.Services.AddScoped<MetricIngestionRunner>();

// Register the notification dispatch runner as scoped
// (created fresh for each dispatch cycle)
builder.Services.AddScoped<NotificationDispatchRunner>();

// Register the background workers
builder.Services.AddHostedService<RuleEvaluationWorker>();
builder.Services.AddHostedService<MetricIngestionWorker>();
builder.Services.AddHostedService<NotificationWorker>();

var host = builder.Build();
host.Run();
