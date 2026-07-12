# API Reference

Base URL: BPS.Web host (default from launch profile in development)

## Submit a job

- Method: POST
- Route: /submit
- Body:

```json
{
  "jobType": "DelayJob",
  "priority": "Medium",
  "payload": "5"
}
```

- Response 200: JobDto

## List jobs

- Method: GET
- Route: /list
- Response 200: array of JobDto

## List jobs by priority

- Method: GET
- Route: /list/{priority}
- Values: High, Medium, Low
- Response 200: array of JobDto

## Job status

- Method: GET
- Route: /status/{jobId}
- Response 200: JobDto
- Response 404: not found

## Control job

- Method: POST
- Route: /status/{jobId}/{action}
- Actions: start, stop, resume
- Response 200: success
- Response 400: invalid action
- Response 409: invalid transition or job missing

## Queue configuration

### Get

- Method: GET
- Route: /configuration
- Response 200:

```json
{
  "maxConcurrentJobs": 2,
  "maxJobDurationSeconds": 120
}
```

### Update

- Method: POST
- Route: /configuration
- Body:

```json
{
  "maxConcurrentJobs": 3,
  "maxJobDurationSeconds": 90
}
```

- Response 200: updated QueueConfiguration
- Response 400: invalid values

## Worker internal endpoints

These are used by BPS.Service and are intentionally simple.

### Claim next job

- Method: POST
- Route: /worker/claim
- Response 200: JobDto
- Response 204: no queued jobs

### Complete running job

- Method: POST
- Route: /worker/{jobId}/complete
- Body:

```json
{
  "status": "Completed",
  "error": ""
}
```

Allowed terminal statuses: Completed, Failed, Stopped.
