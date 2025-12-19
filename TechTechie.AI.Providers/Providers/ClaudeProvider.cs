using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TechTechie.AI.Providers.Interfaces;
using TechTechie.AI.Providers.Models;

namespace TechTechie.AI.Providers.Providers
{
    public class ClaudeProvider : IAIProvider
    {
        private readonly AIProviderConfigModel _config;
        private readonly HttpClient _http;

        public ClaudeProvider(AIProviderConfigModel config, HttpClient http)
        {
            _config = config;
            _http = http;
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("x-api-key", _config.ApiKey);
            _http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        }

        public async Task<string> GenerateAsync(string prompt, Func<string, Task>? onStream = null)
        {
            var body = new
            {
                model = _config.Model,
                prompt = prompt,
                max_tokens = 2000,
                stream = _config.Stream
            };

            var response = await _http.PostAsJsonAsync($"{_config.BaseUrl}/v1/completions", body);

            if (!_config.Stream)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("completion").GetString() ?? "";
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
                if (json.RootElement.TryGetProperty("completion", out var completion))
                {
                    var text = completion.GetString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        output.Append(text);
                        if (onStream != null) await onStream(text);
                    }
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
                stream = _config.Stream,
                max_tokens = 2000
            };

            var response = await _http.PostAsJsonAsync($"{_config.BaseUrl}/v1/messages", body);

            if (!_config.Stream)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("content")[0]
                           .GetProperty("text").GetString() ?? "";
            }

            // Stream mode (SSE)
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
                if (json.RootElement.TryGetProperty("delta", out var delta) &&
                    delta.TryGetProperty("text", out var textNode))
                {
                    var text = textNode.GetString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        output.Append(text);
                        if (onStream != null) await onStream(text);
                    }
                }
            }

            return output.ToString();
        }

        public async Task<List<float>> GetEmbeddingAsync(string input)
        {
            // Claude currently has no official embedding API.
            // You could integrate a 3rd-party embedding model or return an empty vector.
            return await Task.FromResult(new List<float>());
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
