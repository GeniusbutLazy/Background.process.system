# Runbook

## Prerequisites

- .NET SDK for BPS.Web (net10.0 target)
- Visual Studio Build Tools / MSBuild for BPS.Service (.NET Framework 4.8)

## Build

From repository root:

```powershell
dotnet build .\BPS.Web\BPS.Web.csproj
msbuild .\BPS.Service\BPS.Service.csproj /t:Build /p:Configuration=Debug
```

## Run BPS.Web

```powershell
dotnet run --project .\BPS.Web\BPS.Web.csproj
```

Open /queue in browser.

## Run BPS.Service for development

BPS.Service supports interactive console mode when started from terminal.

```powershell
.\BPS.Service\bin\Debug\BPS.Service.exe
```

Press Enter to stop.

## Service configuration

BPS.Service uses App.config settings:

- QueueApiBaseUrl: base URL of BPS.Web API
- WorkerPollIntervalMs: polling interval

File: BPS.Service/App.config

## Smoke test

1. Start BPS.Web.
2. Start BPS.Service.
3. Open /queue.
4. Submit DelayJob with payload 5.
5. Verify state transitions Queued -> Running -> Completed.
6. Submit payload larger than MaxJobDurationSeconds and verify Stopped.

## Known Part A limits

- Queue and job state is in-memory and resets on BPS.Web restart.
- Single sample job type (DelayJob) is implemented.
- Realtime updates are polling-based (not WebSocket).
