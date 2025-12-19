using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTechie.Services.Common.Models;
using TechTechie.Services.Tenant.ServiceInterfaces;
using TechTechie.WebApi.Helpers;

namespace TechTechie.WebApi.Controllers
{
    [Authorize(Policy = "ApiPolicy")]
    [ApiController]
    [Route("api")]
    public class NotificationController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ITenantService _tenantService;
        private readonly IWebHostEnvironment _environment;

        public NotificationController(IConfiguration config, ITenantService tenantService,
            IHttpContextAccessor httpContextAccessor,
            IWebHostEnvironment environment)
        {
            _config = config;
            _tenantService = tenantService;
            _environment = environment;
        }


        [HttpGet("test_mail")]
        public async Task<IActionResult> SendTestMail()
        {
            ResponseMessageModel response = new();
            try
            {
                var signedUser = HttpHelper.GetSignedUser(HttpContext);
                response = await _tenantService.SendTestMail(signedUser);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                response.StatusCode = 400;
            }
            return response.IsSuccess ? Ok(response) : BadRequest(response);

        }

        [HttpGet("send_mail/{id}")]
        public async Task<IActionResult> SendMail(int id)
        {
            ResponseMessageModel response = new();
            try
            {
                var signedUser = HttpHelper.GetSignedUser(HttpContext);

                if (_environment.EnvironmentName.ToLower() != "staging" && _environment.EnvironmentName.ToLower() != "local")
                {
                    response = await _tenantService.SendMail(id, signedUser);
                }
                else
                {
                    throw new Exception("Mail sending is disabled in staging/local environment.");
                }

            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                response.StatusCode = 400;
            }
            return response.IsSuccess ? Ok(response) : BadRequest(response);

        }

        [HttpPost("send_fcm")]
        public async Task<IActionResult> SendFcmNotification([FromBody] FirebaseAdmin.Messaging.Message Message)
        {
            ResponseMessageModel response = new();

            try
            {
                string serviceAccountKeyFile = _config["AppSettings:FirebaseServiceAccount"]!; /// Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

                if (string.IsNullOrEmpty(serviceAccountKeyFile))
                {
                    throw new Exception("Firebase configuration file is not found.");
                }
                if (FirebaseApp.DefaultInstance == null)
                {
                    FirebaseApp.Create(new AppOptions()
                    {
                        Credential = GoogleCredential.FromFile(serviceAccountKeyFile)
                    });
                }

                // Send Message
                string responseFirebase = await FirebaseMessaging.DefaultInstance.SendAsync(Message);

                response.IsSuccess = true;
                response.StatusCode = 200;
                response.Message = "Success";
                response.Data = responseFirebase;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = 400;
                response.Message = "Not able to send the FCM Message";// ex.ToString();
            }
            return response.IsSuccess ? Ok(response) : BadRequest(response);

        }

    }
}
