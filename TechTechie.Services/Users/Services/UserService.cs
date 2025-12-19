using TechTechie.Services.Common.Models;
using TechTechie.Services.Users.Models;
using TechTechie.Services.Users.RepositoryInterfaces;
using TechTechie.Services.Users.ServiceInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Services.Users.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public Task<SignInResponseModel> EmailSignIn(EmailSignInModel emailSignIn)
        {
            return _userRepository.EmailSignIn(emailSignIn);
        }

        public Task<SignInResponseModel> GetUserFromApiKey(string apiKey)
        {
            return _userRepository.GetUserFromApiKey(apiKey);
        }

        public Task<ResponseMessageModel> ForgotPassword(ForgotPasswordModel forgotPassword)
        {
            return _userRepository.ForgotPassword(forgotPassword);
        }

        public Task<SignInResponseModel> GetUserByEmailAndTenant(string tenant_code, string email)
        {
            return _userRepository.GetUserByEmailAndTenant(tenant_code, email);
        }

        public Task<Dictionary<string, object>> ExecuteScimRoute(string Id, string EntityName, string OperationName, string RequestJson, SignedUser user)
        {
            return _userRepository.ExecuteScimRoute(Id, EntityName, OperationName, RequestJson, user);
        }

        public Task<SignedUser> GetUserByPublicAccessCode(string access_code)
        {
            return _userRepository.GetUserByPublicAccessCode(access_code);
        }
    }
}
