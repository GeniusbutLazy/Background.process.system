using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BPS.Service.Queue
{
    internal sealed class WorkerRuntime
    {
        private readonly WorkerClient _client;
        private readonly int _pollIntervalMs;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _runningJobs = new ConcurrentDictionary<string, CancellationTokenSource>();
        private readonly SemaphoreSlim _concurrency;
        private readonly int _maxJobDurationSeconds;

        public WorkerRuntime()
        {
            var baseUrl = ConfigurationManager.AppSettings["QueueApiBaseUrl"] ?? "http://localhost:5171/";
            _pollIntervalMs = ParsePositiveInt(ConfigurationManager.AppSettings["WorkerPollIntervalMs"], 1500);
            _client = new WorkerClient(baseUrl);

            QueueConfiguration configuration;
            try
            {
                configuration = _client.GetConfigurationAsync().GetAwaiter().GetResult();
            }
            catch
            {
                configuration = new QueueConfiguration { MaxConcurrentJobs = 2, MaxJobDurationSeconds = 120 };
            }

            _concurrency = new SemaphoreSlim(Math.Max(1, configuration.MaxConcurrentJobs));
            _maxJobDurationSeconds = Math.Max(5, configuration.MaxJobDurationSeconds);
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
                            await _concurrency.WaitAsync(stopToken).ConfigureAwait(false);
                            _ = ExecuteJobAsync(next, stopToken);
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Worker loop error: " + ex);
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
        }

        private async Task ExecuteJobAsync(JobDto job, CancellationToken stopToken)
        {
            var linked = CancellationTokenSource.CreateLinkedTokenSource(stopToken);
            linked.CancelAfter(TimeSpan.FromSeconds(_maxJobDurationSeconds));
            _runningJobs[job.JobId] = linked;

            try
            {
                await RunJobTypeAsync(job, linked.Token).ConfigureAwait(false);
                await _client.CompleteAsync(job.JobId, "Completed", string.Empty).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                await _client.CompleteAsync(job.JobId, "Stopped", "Cancelled or timed out.").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _client.CompleteAsync(job.JobId, "Failed", ex.Message).ConfigureAwait(false);
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
                if (status != null && string.Equals(status.Status, "Stopped", StringComparison.OrdinalIgnoreCase))
                {
                    running.Value.Cancel();
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
