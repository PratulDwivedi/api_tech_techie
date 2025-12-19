using TechTechie.Services.Common.Models;
using TechTechie.Services.Users.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Services.Dynamic.RepositoryInterfaces
{
    public interface IDynamicRepository
    {
        public Task<ResponseMessageModel> ExecuteRoute(RequestMessageModel requestMessage, SignedUser signedUser);

        public Task<string> GetRouteName(int page_id, int control_id,  SignedUser signedUser);

        public Task<TemplateModel> GetTemplateAsyc(int id, SignedUser signedUser);

    }
}
