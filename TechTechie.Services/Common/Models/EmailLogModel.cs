using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Services.Common.Models
{
    public class EmailLogModel
    {
        public int id { get; set; }
        public string email_to { get; set; } = string.Empty;
        public string email_cc { get; set; } = string.Empty;
        public string email_bcc { get; set; } = string.Empty;
        public string reply_to { get; set; } = string.Empty;
        public string subject { get; set; } = string.Empty;
        public string body { get; set; } = string.Empty;
        public string attachment_hex { get; set; } = string.Empty;
        public string file_name { get; set; } = string.Empty;
    }
}
