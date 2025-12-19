
using Newtonsoft.Json;


namespace TechTechie.Services.Common.Models
{
    public class RequestMessageModel
    {
        [JsonProperty("route_name")]
        public string? RouteName { get; set; }

        [JsonProperty("http_method")]
        public string HttpMethod { get; set; } = "get";

        [JsonProperty("ResponseFormat")]
        public string ResponseFormat { get; set; } = "json";

        [JsonProperty("is_public")]
        public bool IsPublic { get; set; } = false;

        [JsonProperty("Data_encryption_key")]
        public string? DataEncryptionKey { get; set; }

        [JsonProperty("template_id")]
        public int? TemplateId { get; set; }

        [JsonProperty("Data")]
        public Dictionary<string, object>? Data { get; set; }
    }
}
