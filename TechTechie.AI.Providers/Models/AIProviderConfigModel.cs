using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.AI.Providers.Models
{
    public class AIProviderConfigModel
    {
        public required string Provider { get; set; }   // e.g., "ollama", "openai", "claude"
        public required string BaseUrl { get; set; } = string.Empty;    // API endpoint
        public string ApiVersion { get; set; } = string.Empty;// use for azure open ai
        public string ApiKey { get; set; } = string.Empty;     // optional
        public required string Model { get; set; } // model name , gpt40
        public bool Stream { get; set; } = false;              // enable streaming
    }
}
