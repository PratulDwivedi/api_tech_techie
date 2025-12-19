using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Text;
using HandlebarsDotNet;
using TechTechie.Services.Users.Models;
using HtmlAgilityPack;
using System.Collections;
using TechTechie.Services.Common.Models;
using System.Security.Cryptography;

namespace TechTechie.WebApi.Helpers
{
    public class HttpHelper
    {

        public static SignedUser GetSignedUser(HttpContext httpContext)
        {
            SignedUser user = new();

            // Get IConfiguration from DI container via HttpContext
            var config = httpContext.RequestServices.GetRequiredService<IConfiguration>();

            if (httpContext.Request.QueryString.HasValue)
            {
                var token = httpContext.Request.QueryString.Value
                    .Split('&')
                    .SingleOrDefault(x => x.ToLower().Contains("access_token"))?.Split('=')[1];
                if (!string.IsNullOrWhiteSpace(token))
                {
                    user.access_token = token;
                }

                var api_key = httpContext.Request.QueryString.Value
                    .Split('&')
                    .SingleOrDefault(x => x.ToLower().Contains("x-api-key"))?.Split('=')[1];
                if (!string.IsNullOrWhiteSpace(api_key))
                {
                    user.api_key = api_key;
                }

            }

            if (httpContext.User.Identity!.IsAuthenticated)
            {
                var currentUser = httpContext.User;

                if (currentUser.HasClaim(c => c.Type == ClaimTypes.Name))
                {
                    user.tenant_code = currentUser.Claims.FirstOrDefault(c => c.Type == "tenant_code")!.Value;
                    user.tenant_id = Convert.ToInt32(currentUser.Claims.FirstOrDefault(c => c.Type == "tenant_id")!.Value);
                    user.uid = Convert.ToInt32(currentUser.Claims.FirstOrDefault(c => c.Type == "uid")!.Value);
                    user.id = currentUser.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)!.Value;
                    user.email = currentUser.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)!.Value;
                }
                user.access_token = httpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last()!;
                var api_key = httpContext.Request.Headers["x-api-key"].FirstOrDefault()?.Split(" ").Last()!;

                if (string.IsNullOrEmpty(user.access_token))
                {
                    var apiKeyUser = new SignInResponseModel()
                    {
                        email = user.email!,
                        id = user.id!,
                        tenant_code = user.tenant_code!,
                        tenant_id = user.tenant_id!.Value,
                        uid = user.uid!.Value.ToString(),
                        name = user.id!,
                        mobile_no = "",
                        token_expiry_minutes = int.Parse(config["Jwt:ExpiryMinutes"]),

                    };
                    user.access_token = CreateToken(apiKeyUser, config);
                }
                if (!string.IsNullOrWhiteSpace(api_key))
                {
                    user.api_key = api_key;
                }
            }
            else
            {
                throw new Exception("ERR-401 : User is not Authenticated");
            }
            user.request_ip = httpContext.Connection.RemoteIpAddress!.ToString();
            user.request_origin = httpContext.Request.Headers["Origin"];
            return user;

        }
        public static List<Claim> GetClaimsFromUser(SignInResponseModel user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.id),
                new Claim(ClaimTypes.Email, user.email),
                new Claim("uid", user.uid.ToString()),
                new Claim("tenant_id", user.tenant_id.ToString()),
                new Claim("tenant_code", user.tenant_code)
            };

            return claims;
        }
        public static string CreateToken(SignInResponseModel user, IConfiguration config)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var claims = GetClaimsFromUser(user);

            var claimsIdentity = new ClaimsIdentity(claims);

            var privateKeyPem = config["Jwt:PrivateKey"];
            var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem);

            var signingCredentials = new SigningCredentials(
                new RsaSecurityKey(rsa),
                SecurityAlgorithms.RsaSha256
            );

            int tokenExpiryMinutes = user.token_expiry_minutes > 0
                ? user.token_expiry_minutes
                : int.Parse(config["Jwt:ExpiryMinutes"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claimsIdentity,
                Expires = DateTime.UtcNow.AddMinutes(tokenExpiryMinutes),
                Issuer = config["Jwt:Issuer"],
                Audience = config["Jwt:Audience"],
                SigningCredentials = signingCredentials
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);

        }

        public static string GetMimeTypes(string ext)
        {
            Dictionary<string, string> mimetypes = new()
            {
                { ".html", "text/html" },
                { ".htm", "text/html" },
                { ".txt", "text/plain" },
                { ".pdf", "application/pdf" },
                { "pdf", "application/pdf" },
                { ".msg", "application/vnd.ms-outlook" },
                { ".doc", "application/vnd.ms-word" },
                { ".docx", "application/vnd.ms-word" },
                { ".xls", "application/vnd.ms-excel" },
                { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
                { ".ppt", "application/vnd.ms-powerpoint" },
                { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
                { ".png", "image/png" },
                { ".jpg", "image/jpeg" },
                { ".jpeg", "image/jpeg" },
                { ".jfif", "image/jpeg" },
                { ".jif", "image/jpeg" },
                { ".jpe", "image/jpeg" },
                { ".gif", "image/gif" },
                { ".csv", "text/csv" },
                { "image/png", "image/png" },
                { "image/jpeg", "image/jpeg" },
                { "image/gif", "image/gif" },
                { "text/csv", "text/csv" },
                { ".mp4", "video/mp4" },
                { ".mp3", "audio/mpeg" },
                { ".aac", "audio/aac" },
                { ".ogg", "audio/ogg" },
                { ".webp", "image/webp" },
                { ".zip", "application/x-rar-compressed" },
                { ".3gp", "video/3gpp" },
                { ".mov", "video/quicktime" },
                { ".avi", "video/x-msvideo" },
                { ".wmv", "video/x-ms-wmv" },

            };

            string mimeType = "application/octet-stream"; // Defualt mime type

            if (mimetypes.ContainsKey(ext))
            {
                mimeType = mimetypes[ext];
            }

            return mimeType;
        }

        public static string GetContentType(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return GetMimeTypes(ext);
        }

        public static bool IsImage(string fileName)
        {
            return (fileName.EndsWith(".png") || fileName.EndsWith(".jpg") || fileName.EndsWith(".jpeg") || fileName.EndsWith(".tiff"));
        }

        public static Dictionary<string, object> GetQueryStringData(HttpRequest request)
        {
            Dictionary<string, object> Data = new();

            var query = QueryHelpers.ParseQuery(request.QueryString.ToString());

            foreach (var item in query)
            {
                StringValues values = item.Value;

                if (values.Count == 1)
                {
                    string value = values[0];

                    // Handle comma-separated values as arrays
                    if (value.Contains(','))
                    {
                        Data[item.Key] = value;
                    }
                    else
                    {
                        // Try to parse into int, long, double, bool, DateTime, or fallback to string
                        if (int.TryParse(value, out int intVal))
                            Data[item.Key] = intVal;
                        else if (long.TryParse(value, out long longVal))
                            Data[item.Key] = longVal;
                        else if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleVal))
                            Data[item.Key] = doubleVal;
                        else if (bool.TryParse(value, out bool boolVal))
                            Data[item.Key] = boolVal;
                        else if (DateTime.TryParse(value, out DateTime dateValue))
                            Data[item.Key] = dateValue;
                        else
                            Data[item.Key] = value;
                    }
                }
                else
                {
                    Data[item.Key] = values.ToArray();
                }
            }

            return Data;
        }

        public static string GetHtmlFromTemplate(TemplateModel templateModel, Dictionary<string, object> Data)
        {


            // if template render create any issue, test thru given url
            // http://tryhandlebarsjs.com/
            string result;

            // Used for CKEditor
            //templateModel.templateHtml = PreMailer.Net.PreMailer.MoveCssInline(templateModel.templateHtml).Html; ;

            // used for CKEditor styles
            //templateModel.templateHtml = Regex.Replace(templateModel.templateHtml, "<style[^<]*</style\\s*>", "");

            if (Data == null)
            {
                result = templateModel.page_body;

                // Regular expression pattern to match strings between {{}}
                string pattern = @"{{(.*?)}}";

                // Matches all the strings between {{}}
                MatchCollection matches = Regex.Matches(result, pattern);

                // Loop through all the matches and process them
                foreach (Match match in matches)
                {
                    string matchValue = match.Value;
                    string matchResult = match.Groups[1].Value;

                    // Check if the match contains a dot (.)
                    if (matchResult.Contains("."))
                    {
                        // Remove the string before the dot (.)
                        matchResult = matchResult.Substring(matchResult.IndexOf(".") + 1);
                    }

                    // Replace the original match with the processed match result
                    result = result.Replace(matchValue, matchResult);
                }
                return result;
            }
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(templateModel.page_body);
            //try
            //{

            try
            {

                var placeholerNodes = htmlDoc.DocumentNode.SelectNodes("//span[@contenteditable]");

                if (placeholerNodes != null)
                {
                    foreach (HtmlNode spanNode in placeholerNodes)
                    {
                        if (spanNode.Attributes.Contains("Data-key"))
                        {

                            spanNode.InnerHtml = spanNode.Attributes["Data-key"].Value;
                            spanNode.Attributes.Remove("Data-key");
                        }

                    }

                }
            }
            catch (Exception)
            {


            }



            foreach (KeyValuePair<string, object> DataItem in Data)
            {

                try
                {

                    if (IsList(DataItem.Value))
                    {
                        // Error - Sequence contains more than one element. error can come if we found multiple row for same list name

                        try
                        {
                            var seacrhTrNode = htmlDoc.DocumentNode.SelectNodes("//tr").Where(x => x.InnerHtml.Contains(DataItem.Key));


                            var RowNodes = seacrhTrNode.ToList();

                            if (RowNodes != null && RowNodes.Count > 0)
                            {

                                HtmlNode oldRowNode = RowNodes[RowNodes.Count - 1];
                                var tbodyNode = oldRowNode.ParentNode;
                                string rowHtmlOuter = oldRowNode.OuterHtml;
                                rowHtmlOuter = rowHtmlOuter.Replace(DataItem.Key + ".", "");
                                string newNodeStr = "<div>{{#each " + DataItem.Key + "}} " + rowHtmlOuter + " {{/each}}</div>";

                                var newRowNode = HtmlNode.CreateNode(newNodeStr);

                                tbodyNode.RemoveChild(oldRowNode);
                                tbodyNode.AppendChild(newRowNode);

                            }
                            templateModel.page_body = htmlDoc.DocumentNode.InnerHtml.ToString().Replace("<div>{{#each", "{{#each").Replace("{{/each}}</div>", "{{/each}}");

                        }
                        catch (Exception)
                        {
                            continue;
                        }


                    }
                }
                catch (Exception ex)
                {

                    throw new Exception(DataItem.Key + " ; " + ex.Message);
                }

            }

            //File.WriteAllText("templateAfterEach.txt", templateModel.templateHtml);

            // use {{{htmlValue}}} tripple curly braces incase of value is html hyperlink like
            var template = Handlebars.Compile(templateModel.page_body);

            result = template(Data);
            //}
            //catch (Exception ex)
            //{
            //    result = templateModel.templateHtml + ex.Message.ToString();
            //}


            return result;

        }

        public static bool IsList(object o)
        {
            try
            {

                if (o == null) return false;

                if (o.GetType().Name == "JArray") return true;

                if (o.GetType().Name.Contains("List")) return true;

                return o is IList &&
                       o.GetType().IsGenericType &&
                       o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }


        }

    }
}
