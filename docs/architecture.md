# Architecture

## Current delivery scope

This implementation delivers Part A (mandatory) with an architecture that keeps Part B and Part C additions low-risk.

- BPS.Web (ASP.NET Core):
  - Hosts queue management REST API.
  - Hosts queue management Razor UI.
  - Keeps queue state in memory.
- BPS.Service (.NET Framework 4.8 Windows Service):
  - Polls for queued jobs.
  - Claims jobs and executes them concurrently.
  - Reports terminal status back to BPS.Web.

## Component model

### BPS.Web

- Models/Queue:
  - JobPriority, JobStatus
  - JobDto
  - QueueConfiguration
  - SubmitJobRequest
  - WorkerCompleteRequest
- Services/Queue:
  - IQueueStore
  - InMemoryQueueStore
- Controllers:
  - QueueController: REST API endpoints
  - QueueUiController: dashboard page

### BPS.Service

- Queue/WorkerClient:
  - HTTP client for API communication.
- Queue/WorkerRuntime:
  - Poll loop, concurrency semaphore, stop detection, timeout handling.
- Service1:
  - Lifecycle integration for service mode and debug mode.

## Runtime flow

1. User submits a job from UI or POST /submit.
2. Job is stored in memory with status Queued.
3. Worker calls POST /worker/claim, receives next highest-priority queued job.
4. Worker executes DelayJob and sends terminal status via POST /worker/{jobId}/complete.
5. UI polls list/status endpoints and reflects updates.

## Concurrency and controls

- Max concurrent jobs is managed in queue configuration.
- Worker enforces max concurrency via SemaphoreSlim.
- Stop action is propagated by polling status and cancelling the running token.
- Max job duration is enforced in worker runtime with cancellation timeout.

## Extensibility seams for Part B and Part C

- Scheduling seam: claim strategy can be replaced with fairness scheduler.
- Job type seam: current DelayJob can evolve to handler-based plugin model.
- Reliability seam: in-memory store can be swapped for persistent store and dead-letter queue.
- Realtime seam: current UI polling can be replaced by SignalR/WebSocket push with same backend state contracts.
