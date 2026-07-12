Project 2: Background Job Processing System
Overview
Build a scalable background job processing system that allows applications to queue tasks for asynchronous processing. The system should support job scheduling, retry mechanisms, priority queues, and provide real-time status updates via WebSockets. System should be able to support different types of jobs. For example, database upload, CSV conversion, PDF document generation etc.

Part A (Mandatory): Core Job Processing
Requirements
1.	Windows Service
o	Host REST API for Queue Management
o	Process jobs from the queue
o	Concurrent execution of jobs
2.	Queue Management
o	REST API for job submission and queue management
 	/submit
•	Submit a new job
 	/list/{priority}
•	List all jobs or jobs with a priority
 	/status/{jobid}
•	Show status of the specified job
 	/status/{jobid}/{start | stop | resume}
•	Change the state of a job
 	/configuration
•	Manage job queue configuration
o	Configuration of job queue
 	Maximum number of concurrent jobs
 	Maximum duration of jobs
o	Query status of jobs
o	List jobs along with their status
o	Start/Stop/Suspend a specific job
3.	User Interface to provide queue management functions
o	A web UI (Angular, React)
o	Interface to configure job queue
o	Interface to display job status

Part B (Good to Have): Advanced Job Scheduling
Requirements
1.	Job Priority
o	Each job has a priority – High, Medium, Low
o	Jobs are scheduled based on their priority
o	A fairness algorithm to ensure that lower priority jobs get attention
2.	Resiliency and Error Handling
o	Error in any job execution shall not impact other running jobs or jobs in the queue
o	Configurable retry of job execution in case execution fails
o	A Dead Letter Queue to hold jobs that have failed for a defined Maximum retries count
3.	Plugin Design
o	A plugin-based design to define types of jobs and provide an implementation via dynamic loading of assemblies

Part C (Nice to Have): Advanced features
Requirements
1.	Realtime Job updates
o	WebSocket that publishes status of jobs in real-time
o	WebSocket that can stream live logs of a job’s execution
o	Can use a pub/sub model to publish updates to one or more subscribers
o	Display real-time job updates in web UI
2.	Third-party Integration
o	Support calling into third-party systems via Webhook
