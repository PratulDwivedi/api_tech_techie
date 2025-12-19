
using TechTechie.AI.Providers.Models;
using TechTechie.Services.AI.Models;

namespace TechTechie.Services.AI.Interfaces
{
    public interface IAIService
    {
        public Task<AIProviderConfigModel> GetProviderConfigAsync(int configId);

        Task<string> GenerateAsync(int configId, string prompt, Func<string, Task>? onStream = null);

        Task<string> ChatAsync(int configId, string message, List<(string Role, string Content)>? history = null, Func<string, Task>? onStream = null);

        Task<List<float>> GetEmbeddingAsync(int configId, string input);

        Task<byte[]> SpeakAsync(int configId, AISpeakRequestModel request);

        Task SpeakStreamAsync(int configId, AISpeakRequestModel request, Func<byte[], Task>? onChunk = null);


    }

}
