# Background Job Processing System

Background Job Processing System provides queue-based asynchronous job execution with a web management experience.

This repository currently implements Part A from the requirement with design seams for future Part B and Part C enhancements.

## What is implemented

- Windows service worker engine in BPS.Service:
	- Polls queue API
	- Claims jobs
	- Executes jobs concurrently
	- Reports status updates
- Queue API in BPS.Web:
	- /submit
	- /list and /list/{priority}
	- /status/{jobid}
	- /status/{jobid}/{start|stop|resume}
	- /configuration
- Queue management UI in BPS.Web:
	- Submit jobs
	- List/filter jobs
	- Start/stop/resume jobs
	- Configure queue parameters
- One sample job type:
	- DelayJob (payload = delay seconds)

## Solution structure

- BPS.Service: .NET Framework 4.8 Windows Service worker runtime
- BPS.Web: ASP.NET Core MVC host for REST API and queue dashboard
- docs: architecture, API, runbook, and roadmap documentation

## Build

From repository root:

```powershell
dotnet build .\BPS.Web\BPS.Web.csproj
msbuild .\BPS.Service\BPS.Service.csproj /t:Build /p:Configuration=Debug
```

## Run locally

1. Start BPS.Web:

```powershell
dotnet run --project .\BPS.Web\BPS.Web.csproj
```

2. Start BPS.Service in interactive mode (for development):

```powershell
.\BPS.Service\bin\Debug\BPS.Service.exe
```

3. Open queue dashboard:

- http://localhost:5171/queue

## Configuration

BPS.Service configuration file: BPS.Service/App.config

- QueueApiBaseUrl: base URL for BPS.Web
- WorkerPollIntervalMs: worker polling interval

Queue runtime configuration endpoint:

- GET /configuration
- POST /configuration

## Current limitations (Part A)

- In-memory queue state only; data resets when BPS.Web restarts.
- Realtime dashboard updates use polling, not WebSockets.
- DelayJob is the only implemented concrete job type.

## Documentation

- docs/architecture.md
- docs/api.md
- docs/runbook.md
- docs/part-b-c-roadmap.md
