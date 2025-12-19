using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Services.Users.Models
{
    public class SignInResponseModel
    {
        public required int tenant_id { get; set; }
        public required string tenant_code { get; set; }
        public required string uid { get; set; }
        public required string id { get; set; }
        public required string name { get; set; }
        public required string email { get; set; }
        public string? mobile_no { get; set; }
        public required int token_expiry_minutes { get; set; }

        public Dictionary<string, object>? Data { get; set; }

        // put it in the response after successful login
        public string? access_token { get; set; }

       
    }

    public class SignedUser
    {
        public int? tenant_id { get; set; }
        public string? tenant_code { get; set; }
        public int? uid { get; set; }
        public string? id { get; set; }
        public string? user_name { get; set; }
        public string? email { get; set; }
        public string? request_ip { get; set; }
        public string? request_origin { get; set; }

        // null in case of jwt authentication
        public string? api_key { get; set; }

        // null in case of jwt authentication
        public string? access_token { get; set; }

    }
}
