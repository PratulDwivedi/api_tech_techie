using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Services.Common.Models
{
    public class TenantCredentialModel
    {
        public string? credential_id { get; set; }
        public string? name { get; set; }
        public string? api_url { get; set; }
        public string? api_key { get; set; }
        public Dictionary<string, object>? access_control { get; set; }
    }
}
