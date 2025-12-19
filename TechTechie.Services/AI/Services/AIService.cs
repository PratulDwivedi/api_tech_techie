
using TechTechie.AI.Providers.Models;
using TechTechie.AI.Providers.Providers;
using TechTechie.Services.AI.Interfaces;
using TechTechie.Services.AI.Models;

namespace TechTechie.Services.AI.Services
{
    public class AIService : IAIService
    {


        private readonly HttpClient _http;
        private readonly AIProviderFactory _factory;

        public AIService(HttpClient http)
        {
            _http = http;
            _factory = new AIProviderFactory(_http);
        }

        public async Task<AIProviderConfigModel> GetProviderConfigAsync(int configId)
        {
            // TODO: Replace this with a DB or config lookup using Dapper
            //var config = new AIProviderConfigModel
            //{
            //    Provider = AIProviders.Ollama,
            //    BaseUrl = "http://localhost:11434",
            //    Model = "llama3",
            //    Stream = true
            //};

            //var config = new AIProviderConfigModel
            //{
            //    Provider = AIProviders.OpenAI,
            //    BaseUrl = "https://api.openai.com",
            //    Model = "gpt-4o",
            //    Stream = false
            //};

            var config = new AIProviderConfigModel
            {
                Provider = AIProviders.AzureOpenAI,
                BaseUrl = "https://centralindia.tts.speech.microsoft.com",
                Model = "gpt-4o",
                Stream = false
            };

            return await Task.FromResult(config);
        }

        public async Task<string> ChatAsync(int configId, string message, List<(string Role, string Content)>? history = null, Func<string, Task>? onStream = null)
        {

            var config = await GetProviderConfigAsync(configId);

            var provider = _factory.Create(config);

            return await provider.ChatAsync(message, history, onStream);
        }

        public async Task<string> GenerateAsync(int configId, string prompt, Func<string, Task>? onStream = null)
        {
            var config = await GetProviderConfigAsync(configId);

            var provider = _factory.Create(config);

            return await provider.GenerateAsync(prompt, onStream);
        }

        public async Task<List<float>> GetEmbeddingAsync(int configId, string input)
        {
            var config = await GetProviderConfigAsync(configId);

            var provider = _factory.Create(config);

            return await provider.GetEmbeddingAsync(input);
        }

        public async Task<byte[]> SpeakAsync(int configId, AISpeakRequestModel request)
        {
            var config = await GetProviderConfigAsync(configId);

            var provider = _factory.Create(config);

            return await provider.SpeakAsync(request.Text, request.Voice, request.Format);
        }

        public async Task SpeakStreamAsync(int configId, AISpeakRequestModel request, Func<byte[], Task>? onChunk = null)
        {
            var config = await GetProviderConfigAsync(configId);
            var provider = _factory.Create(config);

            await provider.SpeakStreamAsync(
                input: request.Text,
                voice: request.Voice ?? "en-IN-NeerjaNeural",
                format: request.Format ?? "audio-16khz-32kbitrate-mono-mp3",
                onChunk: onChunk
            );
        }
    }
}
