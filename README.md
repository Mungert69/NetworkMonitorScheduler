# NetworkMonitorScheduler

Background scheduler that triggers periodic jobs across the NetworkMonitor
platform (monitor checks, alerts, payments, data housekeeping, AI tasks).

## Entry points
- `Program.cs` bootstraps the host.
- `Startup.cs` registers scheduled hosted services and Rabbit listeners.

## Key folders
- `Services/` scheduled task implementations and Rabbit listeners.
- `Controllers/` HTTP endpoints for health and manual triggers.

## Run locally
```bash
dotnet restore
dotnet run --project NetworkMonitorScheduler.csproj
```

The scheduler publishes events to RabbitMQ based on the configured intervals.
