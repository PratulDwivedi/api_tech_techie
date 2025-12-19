using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using TechTechie.Services.Users.Models;
using TechTechie.Services.Users.ServiceInterfaces;

namespace TechTechie.WebApi.Helpers
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IUserService _userService;
        private const string ApiKeyHeaderName = "x-api-key";

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IUserService userService)
            : base(options, logger, encoder)
        {
            _userService = userService;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string apiKey = null;

            // 1. Check for Bearer token treated as API key if length < 50
            if (Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
            {
                var authHeaderValue = authorizationHeader.FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeaderValue) && authHeaderValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var token = authHeaderValue.Substring("Bearer ".Length).Trim();
                    if (token.Length < 50)
                    {
                        apiKey = token;
                    }
                }
            }

            // 2. Check for API key in the headers
            if (Request.Headers.TryGetValue(ApiKeyHeaderName, out var headerApiKey))
            {
                apiKey = headerApiKey.FirstOrDefault(); // use the first value if multiple exist
            }

            // 3. If not found in headers, check query string
            if (string.IsNullOrEmpty(apiKey) && Request.Query.TryGetValue(ApiKeyHeaderName, out var queryApiKey))
            {
                apiKey = queryApiKey.FirstOrDefault();
            }

            SignInResponseModel user;
            try
            {
                user = await _userService.GetUserFromApiKey(apiKey);
                if (user == null || string.IsNullOrEmpty(user.id))
                {
                    return AuthenticateResult.Fail("Invalid API key.");
                }
            }
            catch (Exception ex)
            {
                // Optionally log ex here
                return AuthenticateResult.Fail("Error valiDataing API key.");
            }

            var claims = HttpHelper.GetClaimsFromUser(user);

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}
