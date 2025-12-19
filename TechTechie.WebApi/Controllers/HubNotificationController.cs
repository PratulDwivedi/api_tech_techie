using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TechTechie.WebApi.Helpers;

namespace TechTechie.WebApi.Controllers
{

    [Authorize(Policy = "ApiPolicy")]
    [ApiController]
    [Route("api")]
    public class HubNotificationController : ControllerBase
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public HubNotificationController(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpGet("notify/{tenant_code}/log_out/{user_id?}")]
        public async Task<IActionResult> LogOutUser(string tenant_code, int user_id)
        {
            await _hubContext.Clients.Group(tenant_code).SendAsync("log_out", tenant_code, user_id);
            return Ok(new { tenant_code, user_id });
        }

        [HttpPost("notify/{tenant_code}/Data/{api_id}")]
        public async Task<IActionResult> NotifyDataPost(string tenant_code, int api_id, [FromBody] Dictionary<string, object> Data)
        {
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(HttpContext.Request.QueryString.ToString());

            foreach (var item in query)
            {
                Data[item.Key] = item.Value;
            }
            await _hubContext.Clients.Group(tenant_code).SendAsync("notify", api_id, Data); Data["api_id"] = api_id;

            // return Data to the client as api response for testing purposes
            Data["api_id"] = api_id;
            Data["tenant_code"] = tenant_code;
            return Ok(Data);
        }
        [HttpGet("notify/{tenant_code}/Data/{api_id}")]
        public async Task<IActionResult> NotifyDataGet(string tenant_code, int api_id)
        {
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(HttpContext.Request.QueryString.ToString());

            Dictionary<string, object> Data = new Dictionary<string, object>();

            foreach (var item in query)
            {
                // Convert StringValues to string or string[]
                Data[item.Key] = item.Value.Count == 1 ? item.Value[0] : item.Value.ToArray();
            }
            Data["api_id"] = api_id;
            Data["tenant_code"] = tenant_code;

            await _hubContext.Clients.Group(tenant_code).SendAsync("notify", api_id, Data);

            return Ok(Data);
        }

    }
}