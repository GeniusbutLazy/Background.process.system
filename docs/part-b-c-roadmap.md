# Part B and Part C Roadmap

## Part B: advanced scheduling and resiliency

1. Priority fairness algorithm
- Replace simple claim ordering with weighted fairness scheduler.
- Keep existing endpoints unchanged.

2. Retry and dead-letter queue
- Add retry metadata per job.
- On failure, retry until threshold.
- Move exhausted jobs to dead-letter storage.

3. Persistent state
- Replace InMemoryQueueStore with persistent implementation.
- Recommended: SQL-backed repository with optimistic locking.

4. Plugin architecture
- Introduce IJobHandler and IJobHandlerFactory contracts.
- Load handlers from assemblies via reflection and whitelist.

## Part C: realtime and integrations

1. Realtime status updates
- Add SignalR hub in BPS.Web.
- Publish job transitions to subscribed UI clients.

2. Live logs streaming
- Emit structured execution logs from worker.
- Stream log events to clients over hub channels.

3. Webhook integration
- Add configurable outbound webhook on terminal states.
- Include retry and signature support.

## Non-breaking migration strategy

- Preserve public routes.
- Add internal abstractions first, then swap implementations behind interfaces.
- Keep job DTO fields backward-compatible and additive.
