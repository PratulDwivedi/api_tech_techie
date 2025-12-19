using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Services.AI.Models
{
    public class AIChatRequestModel
    {
        public int ConfigId { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<AIChatMessage>? History { get; set; }
    }

    public class AIChatMessage
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
    }
}
