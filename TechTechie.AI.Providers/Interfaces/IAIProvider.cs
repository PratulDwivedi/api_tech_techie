
using TechTechie.AI.Providers.Models;

namespace TechTechie.AI.Providers.Interfaces
{
    public interface IAIProvider
    {
        Task<string> GenerateAsync(string prompt, Func<string, Task>? onStream = null);

        Task<string> ChatAsync(string message, List<(string Role, string Content)>? history = null, Func<string, Task>? onStream = null);

        Task<List<float>> GetEmbeddingAsync(string input);

        Task<byte[]> SpeakAsync(string input, string voice, string format);

        Task SpeakStreamAsync(string input, string voice, string format, Func<byte[], Task>? onChunk = null);

    }

}
