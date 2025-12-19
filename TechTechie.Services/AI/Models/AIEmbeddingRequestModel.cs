using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Services.AI.Models
{
    public class AIEmbeddingRequestModel
    {
        public int ConfigId { get; set; }
        public string Input { get; set; } = string.Empty;
    }
}
