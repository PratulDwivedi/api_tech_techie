
using TechTechie.AI.Providers.Interfaces;
using TechTechie.AI.Providers.Models;

namespace TechTechie.AI.Providers.Providers
{
    public class AIProviderFactory
    {
        private readonly HttpClient _http;

        public AIProviderFactory(HttpClient http)
        {
            _http = http;
        }

        public IAIProvider Create(AIProviderConfigModel config)
        {
            return config.Provider.ToLower() switch
            {
                AIProviders.Ollama => new OllamaProvider(config, _http),
                AIProviders.OpenAI => new OpenAIProvider(config, _http),
                AIProviders.Claude => new ClaudeProvider(config, _http),
                AIProviders.AzureOpenAI => new AzureOpenAIProvider(config, _http),
                _ => throw new NotSupportedException($"Provider '{config.Provider}' not supported.")
            };
        }
    }
}
