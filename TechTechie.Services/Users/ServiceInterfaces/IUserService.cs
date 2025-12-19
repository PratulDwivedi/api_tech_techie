using TechTechie.Services.Common.Models;
using TechTechie.Services.Users.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Services.Users.ServiceInterfaces
{
    public interface IUserService
    {
        public Task<SignInResponseModel> EmailSignIn(EmailSignInModel emailSignIn);

        public Task<SignInResponseModel> GetUserFromApiKey(string apiKey);

        public Task<ResponseMessageModel> ForgotPassword(ForgotPasswordModel forgotPassword);

        // use for signed user and forgot password
        public Task<SignInResponseModel> GetUserByEmailAndTenant(string tenant_code, string email);

        public Task<Dictionary<string, object>> ExecuteScimRoute(string Id, string EntityName, string OperationName, string RequestJson, SignedUser user);

        public Task<SignedUser> GetUserByPublicAccessCode(string access_code);

    }
}
