using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.PostgresRepository.Users.Entities
{
    public class UserEntity 
    {
        public int tenant_id { get; set; }  
        public int uid { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string tenant_code { get; set; }
        public string email { get; set; }
        public string mobile_no { get; set; }
        public string password { get; set; }
        public string roles { get; set; }
        public Dictionary<string, object>? data { get; set; }
        public Dictionary<string, object>? meta { get; set; }

    }
}
