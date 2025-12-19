
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TechTechie.AI.Providers.Interfaces;
using TechTechie.AI.Providers.Models;

namespace TechTechie.AI.Providers.Providers
{
    public class OpenAIProvider : IAIProvider
    {
        private readonly AIProviderConfigModel _config;
        private readonly HttpClient _http;

        public OpenAIProvider(AIProviderConfigModel config, HttpClient http)
        {
            _config = config;
            _http = http;
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.ApiKey);
        }

        public async Task<string> GenerateAsync(string prompt, Func<string, Task>? onStream = null)
        {
            var body = new
            {
                model = _config.Model,
                input = prompt,
                stream = _config.Stream,
                max_tokens = 2000
            };

            var response = await _http.PostAsJsonAsync($"{_config.BaseUrl}/v1/completions", body);

            if (!_config.Stream)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("choices")[0]
                           .GetProperty("text").GetString() ?? "";
            }

            // Stream mode (Server-Sent Events style)
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            var output = new StringBuilder();

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("Data:")) continue;

                var Data = line.Substring(5).Trim();
                if (Data == "[DONE]") break;

                var json = JsonDocument.Parse(Data);
                var text = json.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("text").GetString();

                if (!string.IsNullOrEmpty(text))
                {
                    output.Append(text);
                    if (onStream != null) await onStream(text);
                }
            }

            return output.ToString();
        }

        public async Task<string> ChatAsync(string message, List<(string Role, string Content)>? history = null, Func<string, Task>? onStream = null)
        {
            var messages = (history ?? new()).Select(h => new { role = h.Role, content = h.Content }).ToList();
            messages.Add(new { role = "user", content = message });

            var body = new
            {
                model = _config.Model,
                messages = messages,
                stream = _config.Stream
            };

            var response = await _http.PostAsJsonAsync($"{_config.BaseUrl}/v1/chat/completions", body);

            if (!_config.Stream)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("choices")[0]
                           .GetProperty("message")
                           .GetProperty("content").GetString() ?? "";
            }

            // Stream mode
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            var output = new StringBuilder();

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("Data:")) continue;

                var Data = line.Substring(5).Trim();
                if (Data == "[DONE]") break;

                var json = JsonDocument.Parse(Data);
                var delta = json.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("delta")
                    .TryGetProperty("content", out var token)
                    ? token.GetString()
                    : "";

                if (!string.IsNullOrEmpty(delta))
                {
                    output.Append(delta);
                    if (onStream != null) await onStream(delta);
                }
            }

            return output.ToString();
        }

        public async Task<List<float>> GetEmbeddingAsync(string input)
        {
            var body = new
            {
                model = _config.Model ?? "text-embedding-3-small",
                input = input
            };

            var response = await _http.PostAsJsonAsync($"{_config.BaseUrl}/v1/embeddings", body);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            var embeddingArray = doc.RootElement
                .GetProperty("Data")[0]
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(x => x.GetSingle())
                .ToList();

            return embeddingArray;
        }


        public Task<byte[]> SpeakAsync(string input, string voice, string format)
        {
            // This is a placeholder method for providers that don’t implement speech synthesis.
            // Returns an empty byte array to indicate “no audio generated.”
            return Task.FromResult(Array.Empty<byte>());
        }

        public Task SpeakStreamAsync(string input, string voice, string format, Func<byte[], Task>? onChunk = null)
        {
            // This is a placeholder method for providers that don’t support speech streaming.
            // Invokes the callback once with no Data, then completes.

            if (onChunk != null)
            {
                // Send an empty chunk to keep flow consistent
                return onChunk(Array.Empty<byte>());
            }

            return Task.CompletedTask;
        }

    }
}
