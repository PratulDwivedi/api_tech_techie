using Microsoft.AspNetCore.Mvc;
using TechTechie.Services.Common.Models;
using TechTechie.Services.Tenant.ServiceInterfaces;
using TechTechie.Services.Users.Models;
using TechTechie.Services.Users.ServiceInterfaces;
using TechTechie.WebApi.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace TechTechie.WebApi.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Policy = "ApiPolicy")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITenantService _tenantService;
        private readonly IConfiguration _config;

        public UserController(IUserService userService, IConfiguration config, ITenantService tenantService)
        {
            _userService = userService;
            _config = config;
            _tenantService = tenantService;
        }

        [HttpPost("token")]
        [AllowAnonymous]
        public async Task<ActionResult> EmailSignIn([FromBody] EmailSignInModel emailSignIn)
        {
            ResponseMessageModel response = new() { IsSuccess = true, Message = "Success", StatusCode = 200 };

            try
            {
                if (!ModelState.IsValid)
                {
                    throw new Exception("Invalid credentials.");
                }

                //RijndaelModel rijndaelmodel = new RijndaelModel() { Text = emailSignIn.password };
                //emailSignIn.password = RijndaelPlanText.Encrypt(rijndaelmodel);

                var user = await _userService.EmailSignIn(emailSignIn);
                user.access_token = HttpHelper.CreateToken(user, _config);
                response.Data = new[] { user };
            }
            catch (Exception ex)
            {
                response.StatusCode = 400;
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response.IsSuccess ? Ok(response) : BadRequest(response);

        }

        [HttpPost("forgot_password")]
        [AllowAnonymous]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordModel forgotPassword)
        {
            ResponseMessageModel response = new() { IsSuccess = true, Message = "Success", StatusCode = 200 };

            try
            {
                if (!ModelState.IsValid)
                {
                    throw new Exception("Invalid credentials.");
                }
                response = await _userService.ForgotPassword(forgotPassword);
            }
            catch (Exception ex)
            {
                response.StatusCode = 400;
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response.IsSuccess ? Ok(response) : BadRequest(response);

        }

        [HttpGet("tenant-auth-types/{tenant_code?}")]
        [AllowAnonymous]
        public async Task<ActionResult> TenantAuthTypes(string tenant_code)
        {
            ResponseMessageModel response = new() { IsSuccess = true, Message = "Success", StatusCode = 200 };

            try
            {
                var authTypes = await _tenantService.GetTenantAuthTypes(tenant_code);
                response.Data = authTypes;
            }
            catch (Exception ex)
            {
                response.StatusCode = 400;
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response.IsSuccess ? Ok(response) : BadRequest(response);

        }

        [HttpGet("validate")]
        public async Task<ActionResult> GetUserByEmailAndTenant()
        {
            ResponseMessageModel response = new() { IsSuccess = true, Message = "Success", StatusCode = 200 };

            try
            {
                SignedUser signedUser = HttpHelper.GetSignedUser(HttpContext);
                var user = await _userService.GetUserByEmailAndTenant(signedUser.tenant_code!, signedUser.email!);
                user.access_token = HttpHelper.CreateToken(user, _config);
                response.Data = new[] { user };
            }
            catch (Exception ex)
            {
                response.StatusCode = 400;
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response.IsSuccess ? Ok(response) : Unauthorized(response);
        }
    }
}
