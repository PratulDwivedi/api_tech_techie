using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Services.Users.Models
{
    public class AccessControlModel
    {
        public List<string>? user_ids { get; set; }
        public List<string>? role_ids { get; set; }
    }
}
