# SignalEngine.Worker

A dedicated background worker service for rule evaluation and signal generation.

## Overview

The Worker service continuously evaluates active rules against metric data and generates signals when thresholds are breached. It uses a `PeriodicTimer` for efficient, drift-free execution on a configurable interval.

## Architecture

```
SignalEngine.Worker
├── Program.cs                  # Host builder and DI setup
├── Worker.cs                   # BackgroundService with PeriodicTimer
├── Options/
│   └── RuleEvaluationOptions.cs # Configuration options
└── Services/
    └── RuleEvaluationRunner.cs  # MediatR command dispatcher

SignalEngine.Application
└── Rules/Commands/
    ├── EvaluateRulesCommand.cs       # MediatR command
    └── EvaluateRulesCommandHandler.cs # Business logic
```

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=SignalEngine;..."
  },
  "RuleEvaluation": {
    "IntervalSeconds": 300
  }
}
```

### Development Settings (appsettings.Development.json)

```json
{
  "RuleEvaluation": {
    "IntervalSeconds": 60
  }
}
```

## Key Features

- **PeriodicTimer**: Uses .NET 6+ `PeriodicTimer` for efficient timing without drift
- **Scoped DI**: Creates new DI scope for each evaluation cycle (proper DbContext disposal)
- **Exception Safety**: Catches and logs all exceptions without crashing the host
- **MediatR Integration**: Uses CQRS pattern via `EvaluateRulesCommand`
- **Configurable Interval**: Evaluation interval configured via options pattern

## Running the Worker

### From Command Line

```bash
cd src/SignalEngine.Worker
dotnet run
```

### In Development

The worker runs in the background evaluating rules every 60 seconds (development) or 300 seconds (production).

## Rule Evaluation Process

1. **Fetch Active Rules**: Retrieves all active rules from the database
2. **Get Latest Metrics**: For each rule, gets the most recent metric value
3. **Evaluate Conditions**: Compares metric value against rule threshold using the rule's operator
4. **Track Consecutive Breaches**: Uses `SignalState` to track consecutive breaches
5. **Generate Signals**: When consecutive breach count reaches threshold, creates a `Signal`
6. **Dispatch Notifications**: Sends notifications for newly created signals

## Operators Supported

- `GT` - Greater Than
- `LT` - Less Than
- `EQ` - Equal
- `GTE` - Greater Than or Equal
- `LTE` - Less Than or Equal

## Logging

The worker logs at various levels:

- **Information**: Rule evaluation cycle summaries
- **Debug**: Detailed evaluation progress
- **Warning**: Missing metrics, notification failures
- **Error**: Rule evaluation errors (non-fatal)

## Dependencies

- **SignalEngine.Application**: Business logic and MediatR handlers
- **SignalEngine.Infrastructure**: EF Core repositories and services
- **SignalEngine.Domain**: Entity definitions
