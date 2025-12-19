using Microsoft.AspNetCore.Mvc;
using TechTechie.Services.Common.Models;
using TechTechie.Services.Dynamic.ServiceInterfaces;
using TechTechie.Services.Users.ServiceInterfaces;
using TechTechie.WebApi.Helpers;

namespace TechTechie.WebApi.Controllers
{
    [ApiController]
    [Route("api/public")]
    public class PublicController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _environment;
        private readonly IUserService _userService;
        private readonly IDynamicService _dynamicService;

        public PublicController(IConfiguration config, IWebHostEnvironment environment, IUserService userService, IDynamicService dynamicService)
        {
            _config = config;
            _environment = environment;
            _userService = userService;
            _dynamicService = dynamicService;
        }

        [HttpPost("{format?}/{templateId?}")]
        public async Task<ActionResult> Post(string? format, int? templateId, [FromBody] Dictionary<string, object> Data)
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
                ResponseFormat = "json",
                TemplateId = templateId
            };

            if (!string.IsNullOrEmpty(format))
            {
                requestMessage.ResponseFormat = format.ToLower();
            }

            return await ExecuteRoute(requestMessage);

        }

        [HttpGet("{format?}/{templateId?}")]
        public async Task<ActionResult> Get(string? format, int? templateId)
        {
            Dictionary<string, object> Data = HttpHelper.GetQueryStringData(HttpContext.Request);

            RequestMessageModel requestMessage = new()
            {
                Data = Data,
                HttpMethod = "get",
                ResponseFormat = "json",
                TemplateId = templateId
            };

            if (!string.IsNullOrEmpty(format))
            {
                requestMessage.ResponseFormat = format.ToLower();
            }

            return await ExecuteRoute(requestMessage);

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
                requestMessage.ResponseFormat = format.ToLower();
            }

            return await ExecuteRoute(requestMessage);

        }

        private async Task<ActionResult> ExecuteRoute(RequestMessageModel requestMessage)
        {
            ResponseMessageModel response = new() { IsSuccess = true, Message = "Success", StatusCode = 200 };

            try
            {
                var Data = requestMessage.Data;

                string access_code = Data.ContainsKey("access_code") ? Data["access_code"].ToString() : string.Empty;

                // assign actual Data to requestMessage
                requestMessage.Data = Data;

                if (string.IsNullOrEmpty(access_code))
                {
                    return Ok(new ResponseMessageModel
                    {
                        IsSuccess = false,
                        Message = "Access code (access_code) is required.",
                        StatusCode = 400
                    });
                }

                string route_name = Data.ContainsKey("route_name") ? Data["route_name"].ToString() : string.Empty;
                if (string.IsNullOrEmpty(route_name))
                {
                    return Ok(new ResponseMessageModel
                    {
                        IsSuccess = false,
                        Message = "Route Name (route_name) is required.",
                        StatusCode = 400
                    });
                }

                requestMessage.RouteName = route_name;

                var signedUser = await _userService.GetUserByPublicAccessCode(access_code);
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
            return Ok(response);

        }


    }
}
