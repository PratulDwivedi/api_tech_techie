using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Services.Tenants.Models
{
    public class RegisteredTenants
    {
        public static Dictionary<string, string> Tenants { get; set; } = new Dictionary<string, string>()
        {
            { "https://app.assetinfinity.ai", "cloud" }
        };

    }
}
