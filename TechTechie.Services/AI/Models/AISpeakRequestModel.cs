using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Services.AI.Models
{
    public class AISpeakRequestModel
    {
        public int ConfigId { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Voice { get; set; } = "en-IN-NeerjaNeural";
        public string Format { get; set; } = "audio-16khz-32kbitrate-mono-mp3";
    }

}
