using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BPS.Contracts.Queue;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BPS.Service.Queue
{
    internal sealed class WorkerClient
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() }
        };

        public WorkerClient(string baseAddress)
        {
            var handler = new HttpClientHandler
            {
                UseProxy = false,
                Proxy = null
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseAddress.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        public async Task<QueueConfiguration> GetConfigurationAsync()
        {
            var response = await _httpClient.GetAsync("configuration").ConfigureAwait(false);
            await EnsureSuccessStatusCodeWithDetails(response, "GET configuration").ConfigureAwait(false);
            return await DeserializeAsync<QueueConfiguration>(response).ConfigureAwait(false);
        }

        public async Task<JobDto> ClaimAsync()
        {
            var response = await _httpClient.PostAsync("worker/claim", new StringContent("{}", Encoding.UTF8, "application/json")).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            await EnsureSuccessStatusCodeWithDetails(response, "POST worker/claim").ConfigureAwait(false);
            return await DeserializeAsync<JobDto>(response).ConfigureAwait(false);
        }

        public async Task<JobDto> GetStatusAsync(string jobId)
        {
            var response = await _httpClient.GetAsync("status/" + jobId).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            await EnsureSuccessStatusCodeWithDetails(response, "GET status/{jobId}").ConfigureAwait(false);
            return await DeserializeAsync<JobDto>(response).ConfigureAwait(false);
        }

        public async Task CompleteAsync(string jobId, JobStatus status, string error)
        {
            var request = new WorkerCompleteRequest
            {
                Status = status,
                Error = error ?? string.Empty
            };

            var content = Serialize(request);
            var response = await _httpClient.PostAsync("worker/" + jobId + "/complete", content).ConfigureAwait(false);
            await EnsureSuccessStatusCodeWithDetails(response, "POST worker/{jobId}/complete").ConfigureAwait(false);
        }

        private static async Task EnsureSuccessStatusCodeWithDetails(HttpResponseMessage response, string operation)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = string.Empty;
            if (response.Content != null)
            {
                body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            throw new HttpRequestException(operation + " failed with status " + (int)response.StatusCode + " (" + response.StatusCode + "). Body: " + body);
        }

        private static async Task<T> DeserializeAsync<T>(HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(json, JsonSettings);
        }

        private static StringContent Serialize<T>(T value)
        {
            var json = JsonConvert.SerializeObject(value, JsonSettings);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }
    }
}
