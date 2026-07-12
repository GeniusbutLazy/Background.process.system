using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using BPS.Contracts.Queue;
using NLog;

namespace BPS.Service.Queue
{
    internal sealed class WorkerRuntime
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly WorkerClient _client;
        private readonly int _pollIntervalMs;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _runningJobs = new ConcurrentDictionary<string, CancellationTokenSource>();
        private readonly SemaphoreSlim _concurrency;
        private readonly int _maxJobDurationSeconds;
        private int _idleClaimCycles;

        public WorkerRuntime()
        {
            var baseUrl = ConfigurationManager.AppSettings["QueueApiBaseUrl"] ?? "http://localhost:5171/";
            _pollIntervalMs = ParsePositiveInt(ConfigurationManager.AppSettings["WorkerPollIntervalMs"], 1500);
            _client = new WorkerClient(baseUrl);

            QueueConfiguration configuration = new QueueConfiguration { MaxConcurrentJobs = 2, MaxJobDurationSeconds = 120 }; ;
            try
            {
                var remoteconfiguration = _client.GetConfigurationAsync().GetAwaiter().GetResult();
                if(remoteconfiguration != null && remoteconfiguration.MaxConcurrentJobs>0)
                {
                    configuration = remoteconfiguration;
                }

                    
            }
            catch
            {
                
            }
            

            _concurrency = new SemaphoreSlim(Math.Max(1, configuration.MaxConcurrentJobs));
            _maxJobDurationSeconds = Math.Max(5, configuration.MaxJobDurationSeconds);
            Logger.Info("Worker runtime initialized. BaseUrl={0}, PollIntervalMs={1}, MaxConcurrency={2}, MaxJobDurationSeconds={3}", baseUrl, _pollIntervalMs, Math.Max(1, configuration.MaxConcurrentJobs), _maxJobDurationSeconds);
        }

        public async Task RunAsync(CancellationToken stopToken)
        {
            while (!stopToken.IsCancellationRequested)
            {
                try
                {
                    await CancelStoppedJobsAsync().ConfigureAwait(false);

                    if (_concurrency.CurrentCount > 0)
                    {
                        var next = await _client.ClaimAsync().ConfigureAwait(false);
                        if (next != null)
                        {
                            _idleClaimCycles = 0;
                            Logger.Info("Claimed job {0} with type {1}.", next.JobId, next.JobType);
                            await _concurrency.WaitAsync(stopToken).ConfigureAwait(false);
                            _ = ExecuteJobAsync(next, stopToken);
                        }
                        else
                        {
                            _idleClaimCycles++;
                            if (_idleClaimCycles % 20 == 0)
                            {
                                Logger.Info("Worker poll heartbeat: no queued jobs found after {0} claim attempts.", _idleClaimCycles);
                            }
                        }
                    }
                    else
                    {
                        Logger.Warn("Worker poll skipped because concurrency count is zero.");
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Worker loop error.");
                }

                try
                {
                    await Task.Delay(_pollIntervalMs, stopToken).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            foreach (var kv in _runningJobs)
            {
                kv.Value.Cancel();
            }

            Logger.Info("Worker runtime loop exited.");
        }

        private async Task ExecuteJobAsync(JobDto job, CancellationToken stopToken)
        {
            var linked = CancellationTokenSource.CreateLinkedTokenSource(stopToken);
            linked.CancelAfter(TimeSpan.FromSeconds(_maxJobDurationSeconds));
            _runningJobs[job.JobId] = linked;

            try
            {
                Logger.Info("Executing job {0} with type {1}.", job.JobId, job.JobType);
                await RunJobTypeAsync(job, linked.Token).ConfigureAwait(false);
                await _client.CompleteAsync(job.JobId, JobStatus.Completed, string.Empty).ConfigureAwait(false);
                Logger.Info("Job {0} completed.", job.JobId);
            }
            catch (TaskCanceledException)
            {
                await _client.CompleteAsync(job.JobId, JobStatus.Stopped, "Cancelled or timed out.").ConfigureAwait(false);
                Logger.Warn("Job {0} stopped due to cancellation or timeout.", job.JobId);
            }
            catch (Exception ex)
            {
                await _client.CompleteAsync(job.JobId, JobStatus.Failed, ex.Message).ConfigureAwait(false);
                Logger.Error(ex, "Job {0} failed.", job.JobId);
            }
            finally
            {
                CancellationTokenSource removed;
                _runningJobs.TryRemove(job.JobId, out removed);
                if (removed != null)
                {
                    removed.Dispose();
                }
                _concurrency.Release();
            }
        }

        private async Task RunJobTypeAsync(JobDto job, CancellationToken token)
        {
            var delaySeconds = 5;
            if (!string.IsNullOrWhiteSpace(job.Payload))
            {
                int parsed;
                if (int.TryParse(job.Payload, out parsed) && parsed > 0)
                {
                    delaySeconds = parsed;
                }
            }

            if (!string.Equals(job.JobType, "DelayJob", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Unsupported job type: " + job.JobType);
            }

            for (var i = 0; i < delaySeconds; i++)
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(1000, token).ConfigureAwait(false);
            }
        }

        private async Task CancelStoppedJobsAsync()
        {
            foreach (var running in _runningJobs)
            {
                var status = await _client.GetStatusAsync(running.Key).ConfigureAwait(false);
                if (status != null && status.Status == JobStatus.Stopped)
                {
                    running.Value.Cancel();
                    Logger.Info("Cancellation requested for running job {0} due to external stop state.", running.Key);
                }
            }
        }

        private static int ParsePositiveInt(string value, int fallback)
        {
            int parsed;
            if (int.TryParse(value, out parsed) && parsed > 0)
            {
                return parsed;
            }

            return fallback;
        }
    }
}
