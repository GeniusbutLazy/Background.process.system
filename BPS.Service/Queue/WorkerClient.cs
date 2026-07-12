using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace BPS.Service.Queue
{
    internal sealed class WorkerClient
    {
        private readonly HttpClient _httpClient;

        public WorkerClient(string baseAddress)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseAddress.TrimEnd('/') + "/")
            };
        }

        public async Task<QueueConfiguration> GetConfigurationAsync()
        {
            var response = await _httpClient.GetAsync("configuration").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await DeserializeAsync<QueueConfiguration>(response).ConfigureAwait(false);
        }

        public async Task<JobDto> ClaimAsync()
        {
            var response = await _httpClient.PostAsync("worker/claim", new StringContent("{}", Encoding.UTF8, "application/json")).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await DeserializeAsync<JobDto>(response).ConfigureAwait(false);
        }

        public async Task<JobDto> GetStatusAsync(string jobId)
        {
            var response = await _httpClient.GetAsync("status/" + jobId).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await DeserializeAsync<JobDto>(response).ConfigureAwait(false);
        }

        public async Task CompleteAsync(string jobId, string status, string error)
        {
            var request = new WorkerCompleteRequest
            {
                Status = status,
                Error = error ?? string.Empty
            };

            var content = Serialize(request);
            var response = await _httpClient.PostAsync("worker/" + jobId + "/complete", content).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        private static async Task<T> DeserializeAsync<T>(HttpResponseMessage response)
        {
            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var serializer = new DataContractJsonSerializer(typeof(T));
            return (T)serializer.ReadObject(stream);
        }

        private static StringContent Serialize<T>(T value)
        {
            using (var stream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                serializer.WriteObject(stream, value);
                var json = Encoding.UTF8.GetString(stream.ToArray());
                return new StringContent(json, Encoding.UTF8, "application/json");
            }
        }
    }
}
