using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTechie.Services.Common.Models;
using TechTechie.Services.Dynamic.ServiceInterfaces;
using TechTechie.Services.Tenant.ServiceInterfaces;
using TechTechie.Services.Users.Models;
using TechTechie.Services.Users.ServiceInterfaces;
using TechTechie.WebApi.Helpers;

namespace TechTechie.WebApi.Controllers
{
    [Authorize(Policy = "ApiPolicy")]
    [ApiController]
    [Route("api")]
    public class DynamicController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IDynamicService _dynamicService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserService _userService;
        private readonly ITenantService _tenantService;
        private readonly IWebHostEnvironment _environment;

        public DynamicController(IConfiguration config, IDynamicService dynamicService,
            IUserService userService, ITenantService tenantService,
            IHttpContextAccessor httpContextAccessor,
            IWebHostEnvironment environment)
        {
            _config = config;

            _dynamicService = dynamicService;
            _userService = userService;
            _tenantService = tenantService;
            _httpContextAccessor = httpContextAccessor;
            _environment = environment;
        }

        [HttpPost("{routeName?}/{format?}/{templateId?}")]
        public async Task<ActionResult> Post(string routeName, string? format, int? templateId, [FromBody] Dictionary<string, object> Data)
        {
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(HttpContext.Request.QueryString.ToString());

            foreach (var item in query)
            {
                Data[item.Key] = item.Value;
            }

            RequestMessageModel requestMessage = new()
            {
                Data = Data,
                HttpMethod = "post",
                RouteName = routeName,
                ResponseFormat = "json",
                TemplateId = templateId

            };

            if (!string.IsNullOrEmpty(format))
            {
                if (format.ToLower() == "offline")
                {
                    requestMessage.ResponseFormat = "json";
                }
                else
                {
                    requestMessage.ResponseFormat = format.ToLower();
                }
            }

            return await ExecuteRoute(requestMessage);

        }

        [HttpPut("{routeName?}/{format?}/{templateId?}")]
        public async Task<ActionResult> Put(string routeName, string? format, int? templateId, [FromBody] Dictionary<string, object> Data)
        {
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(HttpContext.Request.QueryString.ToString());

            foreach (var item in query)
            {
                Data[item.Key] = item.Value;
            }

            RequestMessageModel requestMessage = new()
            {
                Data = Data,
                HttpMethod = "post",
                RouteName = routeName,
                ResponseFormat = "json",
                TemplateId = templateId

            };

            if (!string.IsNullOrEmpty(format))
            {
                if (format.ToLower() == "offline")
                {
                    requestMessage.ResponseFormat = "json";
                }
                else
                {
                    requestMessage.ResponseFormat = format.ToLower();
                }
            }

            return await ExecuteRoute(requestMessage);

        }


        [HttpGet("{routeName?}/{format?}/{templateId?}")]
        public async Task<ActionResult> Get(string routeName, string? format, int? templateId)
        {
            Dictionary<string, object> Data = HttpHelper.GetQueryStringData(HttpContext.Request);

            RequestMessageModel requestMessage = new()
            {
                Data = Data,
                HttpMethod = "get",
                RouteName = routeName,
                ResponseFormat = "json",
                TemplateId = templateId

            };
            var signedUser = HttpHelper.GetSignedUser(HttpContext);

            // use in for field rule/condition to load the values based on api is selected
            if (routeName.ToLower() == "dynamic")
            {
                try
                {
                    int page_id = 0;
                    int control_id = 0;
                    if (Data.ContainsKey("binding_list_page_id"))
                    {
                        page_id = Convert.ToInt32(Data["binding_list_page_id"]);
                        requestMessage.RouteName = await _dynamicService.GetRouteName(page_id, control_id, signedUser);
                    }
                    if (Data.ContainsKey("control_id"))
                    {
                        control_id = Convert.ToInt32(Data["control_id"]);
                        requestMessage.RouteName = await _dynamicService.GetRouteName(page_id, control_id, signedUser);

                    }
                    if (string.IsNullOrEmpty(requestMessage.RouteName))
                    {
                        throw new Exception("Binding list page id is required.");
                    }

                }
                catch (Exception)
                {
                    var response = new ResponseMessageModel() { IsSuccess = false, StatusCode = 400, Message = "Api id route is not found." };
                    return BadRequest(response);
                }

            }

            if (!string.IsNullOrEmpty(format))
            {
                if (format.ToLower() == "offline")
                {
                    requestMessage.ResponseFormat = "json";
                }
                else
                {
                    requestMessage.ResponseFormat = format.ToLower();
                }
            }

            return await ExecuteRoute(requestMessage, signedUser);

        }


        [HttpDelete("{routeName?}/{format?}")]
        public async Task<ActionResult> Delete(string routeName, string? format)
        {
            Dictionary<string, object> Data = new();

            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(HttpContext.Request.QueryString.ToString());

            foreach (var item in query)
            {
                Data[item.Key] = item.Value;
            }

            RequestMessageModel requestMessage = new()
            {
                Data = Data,
                HttpMethod = "delete",
                RouteName = routeName,
                ResponseFormat = "json"

            };

            if (!string.IsNullOrEmpty(format))
            {
                if (format.ToLower() == "offline")
                {
                    requestMessage.ResponseFormat = "json";
                }
                else
                {
                    requestMessage.ResponseFormat = format.ToLower();
                }
            }

            return await ExecuteRoute(requestMessage);

        }

        private async Task<ActionResult> ExecuteRoute(RequestMessageModel requestMessage, SignedUser? signedUser = null)
        {
            ResponseMessageModel response = new() { IsSuccess = true, Message = "Success", StatusCode = 200 };

            try
            {
                if (signedUser is null)
                {
                    signedUser = HttpHelper.GetSignedUser(HttpContext);
                }

                response = await _dynamicService.ExecuteRoute(requestMessage, signedUser);

                if (response.IsSuccess && response.Data is not null
                    && requestMessage.TemplateId is not null
                    && requestMessage.TemplateId > 0)
                {
                    var template = await _dynamicService.GetTemplateAsyc(requestMessage.TemplateId.Value, signedUser);


                    if (template.id == 0)
                    {
                        response.IsSuccess = false;
                        response.StatusCode = 400;
                        response.Message = "Template is not found.";
                    }
                    else
                    {
                        var pageData = response.Data as List<Dictionary<string, object>>;



                        if (pageData is null || pageData!.Count == 0)
                        {
                            response.IsSuccess = false;
                            response.Message = "No Data found to get the template output.";
                            response.StatusCode = 400;
                        }
                        else
                        {
                            var resultHtml = HttpHelper.GetHtmlFromTemplate(template, pageData[0]);
                            template.page_body = resultHtml;

                            if (response.IsSuccess)
                            {
                                if (requestMessage.ResponseFormat == "html")
                                {
                                    return Content(resultHtml, "text/html");
                                }
                                else if (requestMessage.ResponseFormat == "pdf")
                                {
                                    Response.Headers.Add("content-disposition", "inline;filename=" + requestMessage.RouteName + ".pdf");
                                    byte[] buffer = PdfItext7Helper.GetPdfBytesFromHtml(template);

                                    return File(buffer, "application/pdf");
                                }
                                else
                                {
                                    throw new Exception("Response format is invalid.");
                                }
                            }
                            else
                            {
                                throw new Exception("Error when rendering page.");
                            }


                        }
                    }
                }

            }
            catch (Exception ex)
            {
                response.StatusCode = 400;
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response.IsSuccess ? Ok(response) : BadRequest(response);

        }

        [HttpGet("iphost")]
        [AllowAnonymous]
        public IActionResult KeyGetClientIpAndHost()
        {

            var domainName = _httpContextAccessor.HttpContext!.Request.Headers["Origin"]!;
            var ipAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress!.ToString();

            List<Dictionary<string, object>> IpInfoList = new();
            IpInfoList.Add(new Dictionary<string, object>
                {
                    { "domain_name", domainName },
                    { "ip_address", ipAddress }
                });


            ResponseMessageModel response = new()
            {
                IsSuccess = true,
                StatusCode = 200,
                Message = "Success",
                Data = IpInfoList

            };
            return Ok(response);
        }


    }
}
