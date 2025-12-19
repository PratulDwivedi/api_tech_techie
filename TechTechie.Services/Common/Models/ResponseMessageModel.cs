using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TechTechie.Services.Common.Models
{
    public class ResponseMessageModel
    {
        [JsonProperty("is_success")]
        public bool IsSuccess { get; set; } = false;

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty("status_code")]
        public int StatusCode { get; set; } = 400;

        [JsonProperty("data")]
        public object? Data { get; set; }
    }
}
