using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTechie.Services.Common.Models;
using TechTechie.Services.Dynamic.ServiceInterfaces;
using TechTechie.WebApi.Helpers;

namespace TechTechie.WebApi.Controllers
{

    [Authorize(Policy = "ApiPolicy")]
    [ApiController]
    public class RfidController : ControllerBase
    {
        private readonly IDynamicService _dynamicService;

        public RfidController(IDynamicService dynamicService)
        {
            _dynamicService = dynamicService;
        }


        [HttpPost("~/api/rfid/{protocol}")]
        public async Task<IActionResult> SaveRfidData(string protocol, [FromBody] Dictionary<string, dynamic> payload)
        {
            ResponseMessageModel response = new() { IsSuccess = true, Message = "Success", StatusCode = 200 };
            try
            {
                Dictionary<string, dynamic> data = new Dictionary<string, dynamic>();

                var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(HttpContext.Request.QueryString.ToString());

                foreach (var item in query)
                {
                    data[item.Key] = item.Value;
                }

                data["data"] = payload;
                data["protocol"] = protocol;

                RequestMessageModel requestMessage = new()
                {
                    Data = data,
                    HttpMethod = "post",
                    RouteName = "rfid_log",
                    ResponseFormat = "json"

                };

                var signedUser = HttpHelper.GetSignedUser(HttpContext);
                if (signedUser == null)
                {
                    throw new UnauthorizedAccessException("User is not authenticated.");
                }

                response = await _dynamicService.ExecuteRoute(requestMessage, signedUser);


            }
            catch (UnauthorizedAccessException uaEx)
            {
                response.StatusCode = 400;
                response.IsSuccess = false;
                response.Message = uaEx.Message;
            }
            catch (Exception ex)
            {
                response.StatusCode = 400;
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response.IsSuccess ? Ok(response) : BadRequest(response);

        }
    }
}
