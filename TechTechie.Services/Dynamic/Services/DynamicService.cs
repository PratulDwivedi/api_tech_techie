using TechTechie.Services.Common.Models;
using TechTechie.Services.Dynamic.RepositoryInterfaces;
using TechTechie.Services.Dynamic.ServiceInterfaces;
using TechTechie.Services.Users.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Services.Dynamic.Services
{
    public class DynamicService : IDynamicService
    {
        private readonly IDynamicRepository _dynamicRepository;

        public DynamicService(IDynamicRepository dynamicRepository)
        {
            _dynamicRepository = dynamicRepository;
        }

        public Task<ResponseMessageModel> ExecuteRoute(RequestMessageModel requestMessage, SignedUser signedUser)
        {
            return _dynamicRepository.ExecuteRoute(requestMessage, signedUser);
        }

        public Task<string> GetRouteName(int page_id, int control_id, SignedUser signedUser)
        {
           return _dynamicRepository.GetRouteName(page_id, control_id, signedUser);
        }

        public Task<TemplateModel> GetTemplateAsyc(int template_id, SignedUser signedUser)
        {
            return _dynamicRepository.GetTemplateAsyc(template_id, signedUser);
        }
    }
}
