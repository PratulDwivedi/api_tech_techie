using TechTechie.Services.Common.Models;
using TechTechie.Services.Tenants.Models;
using TechTechie.Services.Users.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Services.Tenants.RepositoryInterfaces
{
    public interface ITenantRepository
    {
        public Task<List<Dictionary<string, object>>> GetTenantAuthTypes(string tenant_code);

        public Task<ResponseMessageModel> SendTestMail( SignedUser signedUser);

        public Task<ResponseMessageModel> SendMail(int id, SignedUser signedUser);

    }
}
