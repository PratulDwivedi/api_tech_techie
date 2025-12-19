using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Common
{
    public static class Utils
    {
        public static string GetMimeType(string format)
        {
            if (format.Contains("mp3", StringComparison.OrdinalIgnoreCase))
                return "audio/mpeg";
            if (format.Contains("wav", StringComparison.OrdinalIgnoreCase) ||
                format.Contains("pcm", StringComparison.OrdinalIgnoreCase))
                return "audio/wav";
            if (format.Contains("ogg", StringComparison.OrdinalIgnoreCase))
                return "audio/ogg";
            return "application/octet-stream";
        }
    }
}
