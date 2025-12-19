using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Services.Tenants.Models
{
    public class EncryptDecruptModel
    {
        public string Key { get; set; } = "3FAC086C879F4F58221D8CAF38A02CA7"; // default key
        public string Value { get; set; }
        public string EncryptedValue { get; set; }

    }
}
