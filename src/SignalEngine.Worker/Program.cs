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

// Register the rule evaluation runner as scoped
// (created fresh for each evaluation cycle)
builder.Services.AddScoped<RuleEvaluationRunner>();

// Register the background worker
builder.Services.AddHostedService<RuleEvaluationWorker>();

var host = builder.Build();
host.Run();
