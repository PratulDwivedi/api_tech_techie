using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TechTechie.AI.Providers.Interfaces;
using TechTechie.AI.Providers.Models;

namespace TechTechie.AI.Providers.Providers
{
    public class AzureOpenAIProvider : IAIProvider
    {
        private readonly AIProviderConfigModel _config;
        private readonly HttpClient _http;

        public AzureOpenAIProvider(AIProviderConfigModel config, HttpClient http)
        {
            _config = config;
            _http = http;

            // Azure uses api-key header, not Bearer
            _http.DefaultRequestHeaders.Remove("Authorization");
            _http.DefaultRequestHeaders.Remove("api-key");
            _http.DefaultRequestHeaders.Add("api-key", _config.ApiKey);



        }

        private string GetCompletionsUrl(string endpointType)
        {
            // Azure format:
            // https://{resource}.openai.azure.com/openai/deployments/{deploymentName}/{endpointType}?api-version={version}
            var apiVersion = string.IsNullOrWhiteSpace(_config.ApiVersion)
                ? "2024-06-01" // safe default version
                : _config.ApiVersion;

            return $"{_config.BaseUrl}/openai/deployments/{_config.Model}/{endpointType}?api-version={apiVersion}";
        }

        private string GetTtsUrl() =>
             $"{_config.BaseUrl.TrimEnd('/')}/cognitiveservices/v1";

        public async Task<string> GenerateAsync(string prompt, Func<string, Task>? onStream = null)
        {
            var url = GetCompletionsUrl("completions");
            var body = new
            {
                prompt = prompt,
                max_tokens = 2000,
                stream = _config.Stream
            };

            var response = await _http.PostAsJsonAsync(url, body);
            response.EnsureSuccessStatusCode();

            if (!_config.Stream)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("choices")[0]
                           .GetProperty("text").GetString() ?? "";
            }

            // Stream mode (SSE style)
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
            var url = GetCompletionsUrl("chat/completions");

            var messages = (history ?? new()).Select(h => new { role = h.Role, content = h.Content }).ToList();
            messages.Add(new { role = "user", content = message });

            var body = new
            {
                messages = messages,
                stream = _config.Stream
            };

            var response = await _http.PostAsJsonAsync(url, body);
            response.EnsureSuccessStatusCode();

            if (!_config.Stream)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("choices")[0]
                           .GetProperty("message")
                           .GetProperty("content").GetString() ?? "";
            }

            // Streamed mode
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
            var url = GetCompletionsUrl("embeddings");

            var body = new
            {
                input = input
            };

            var response = await _http.PostAsJsonAsync(url, body);
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

        public async Task<byte[]> SpeakAsync(string input, string voice, string format)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Text cannot be null or empty.", nameof(input));

            if (string.IsNullOrWhiteSpace(input))
                voice = "en-IN-NeerjaNeural";

            if (string.IsNullOrWhiteSpace(input))
                format = "audio-16khz-32kbitrate-mono-mp3";


            var ssml = $@"
            <speak version='1.0' xml:lang='en-IN'>
                <voice xml:lang='en-IN' xml:gender='Female' name='{voice}'>
                    {System.Security.SecurityElement.Escape(input)}
                </voice>
            </speak>";

            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _config.ApiKey);
            _http.DefaultRequestHeaders.Add("User-Agent", "PratulApp/1.0"); // ✅ Required by Azure

            _http.DefaultRequestHeaders.Add("X-Microsoft-OutputFormat", format);

            var content = new StringContent(ssml, Encoding.UTF8, "application/ssml+xml");

            using var response = await _http.PostAsync(GetTtsUrl(), content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync();
        }


        /// <summary>
        /// Streams speech synthesis output in real-time.
        /// Useful for playing as the speech is generated.
        /// </summary>
        public async Task SpeakStreamAsync(string input, string voice = "", string format = "", Func<byte[], Task>? onChunk = null)
        {


            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Text cannot be null or empty.", nameof(input));

            if (string.IsNullOrWhiteSpace(input))
                voice = "en-IN-NeerjaNeural";

            if (string.IsNullOrWhiteSpace(input))
                format = "audio-16khz-32kbitrate-mono-mp3";



            var ssml = $@"
            <speak version='1.0' xml:lang='en-IN'>
                <voice xml:lang='en-IN' xml:gender='Female' name='{voice}'>
                    {System.Security.SecurityElement.Escape(input)}
                </voice>
            </speak>";

            var request = new HttpRequestMessage(HttpMethod.Post, GetTtsUrl())
            {
                Content = new StringContent(ssml, Encoding.UTF8, "application/ssml+xml")
            };

            request.Headers.TryAddWithoutValidation("X-Microsoft-OutputFormat", format);

            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            var buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
            {
                var chunk = buffer[..bytesRead];
                if (onChunk != null)
                    await onChunk(chunk);
            }
        }

    }
}
