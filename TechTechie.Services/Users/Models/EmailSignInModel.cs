using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Services.Users.Models
{
    public class EmailSignInModel
    {
        public required string tenant_code { get; set; }
        public required string email { get; set; }
        public required string password { get; set; }
    }

    public class ForgotPasswordModel
    {
        public required string tenant_code { get; set; }
        public required string email { get; set; }
     
    }
}
