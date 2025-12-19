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
    public class OllamaProvider : IAIProvider
    {
        private readonly AIProviderConfigModel _config;
        private readonly HttpClient _http;

        public OllamaProvider(AIProviderConfigModel config, HttpClient http)
        {
            _config = config;
            _http = http;
        }

        public async Task<string> GenerateAsync(string prompt, Func<string, Task>? onStream = null)
        {
            var body = new { model = _config.Model, prompt = prompt, stream = _config.Stream };
            var response = await _http.PostAsJsonAsync($"{_config.BaseUrl}/api/generate", body);

            if (!_config.Stream)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("response").GetString() ?? "";
            }

            // Stream mode
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            var output = new StringBuilder();

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var json = JsonDocument.Parse(line);
                var text = json.RootElement.TryGetProperty("response", out var resp)
                    ? resp.GetString()
                    : "";
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

            var body = new { model = _config.Model, messages = messages, stream = _config.Stream };
            var response = await _http.PostAsJsonAsync($"{_config.BaseUrl}/api/chat", body);

            if (!_config.Stream)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "";
            }

            // Stream chat
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            var output = new StringBuilder();

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var json = JsonDocument.Parse(line);
                var text = json.RootElement.TryGetProperty("message", out var msg)
                    ? msg.GetProperty("content").GetString()
                    : "";
                if (!string.IsNullOrEmpty(text))
                {
                    output.Append(text);
                    if (onStream != null) await onStream(text);
                }
            }

            return output.ToString();
        }


        public async Task<List<float>> GetEmbeddingAsync(string input)
        {
            var body = new
            {
                model = _config.Model,
                prompt = input
            };

            var response = await _http.PostAsJsonAsync($"{_config.BaseUrl}/api/embeddings", body);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            return doc.RootElement.GetProperty("embedding")
                .EnumerateArray()
                .Select(x => x.GetSingle())
                .ToList();
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
