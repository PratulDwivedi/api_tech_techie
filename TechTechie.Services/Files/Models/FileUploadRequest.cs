using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Services.Files.Models
{
    public class FileUploadRequest
    {
        public string FileName { get; set; } = default!;
        public string ContentType { get; set; } = default!;
        public Stream Content { get; set; } = default!;
    }

}
