using TechTechie.Services.Common.Models;
using TechTechie.Services.Users.Models;


namespace TechTechie.Services.Dynamic.ServiceInterfaces
{
    public interface IDynamicService
    {
        public Task<ResponseMessageModel> ExecuteRoute(RequestMessageModel requestMessage, SignedUser signedUser);

        public Task<string> GetRouteName(int page_id, int control_id, SignedUser signedUser);

        public Task<TemplateModel> GetTemplateAsyc(int id, SignedUser signedUser);
    }
}
