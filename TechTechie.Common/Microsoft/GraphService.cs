using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TechTechie.Common.Models;

namespace TechTechie.Common.Microsoft
{
    public class GraphService
    {

        private static HttpClient _httpClient = new HttpClient();

        public async Task<MailResponse> SendEmailAsync(
            OAuthModel authModel,
            MailModel mail)
        {
            try
            {

                // 1. Get OAuth2 token
                string accessToken = await GetAccessTokenAsync(authModel);

                if (string.IsNullOrEmpty(accessToken))
                {
                    throw new Exception("❌ Failed to get access token");

                }

                // 2. Create email object using proper JSON serialization
                var emailPayload = BuildEmailPayload(mail);

                // 3. Send email via Graph REST API
                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"https://graph.microsoft.com/v1.0/users/{mail.FromEMail}/sendMail"
                );

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = new StringContent(emailPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new MailResponse() { IsSuccess = true, Message = "✅ Email sent successfully!" };

                }
                else
                {
                    return new MailResponse() { IsSuccess = false, Message = responseBody };

                }
            }
            catch (Exception ex)
            {
                return new MailResponse() { IsSuccess = false, Message = $"❌ Exception: {ex.Message}" };

            }
        }

        /// <summary>
        /// Get OAuth2 access token with caching
        /// </summary>
        public async Task<string> GetAccessTokenAsync(OAuthModel authModel)
        {
            try
            {
                FormUrlEncodedContent formData;
                var tokenUrl = $"https://login.microsoftonline.com/{authModel.TenantId}/oauth2/v2.0/token";

                if (authModel.GrantType == "client_credentials")
                {
                    formData = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", "client_credentials"),
                        new KeyValuePair<string, string>("client_id", authModel.ClientId),
                        new KeyValuePair<string, string>("client_secret", authModel.ClientSecret),
                        new KeyValuePair<string, string>("scope", authModel.Scope)
                    });
                }
                else
                {
                    formData = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", authModel.GrantType),
                        new KeyValuePair<string, string>("client_id", authModel.ClientId),
                        new KeyValuePair<string, string>("client_secret", authModel.ClientSecret),
                        new KeyValuePair<string, string>("scope", authModel.Scope)
                    });
                }



                var tokenResponse = await _httpClient.PostAsync(tokenUrl, formData);
                string tokenJson = await tokenResponse.Content.ReadAsStringAsync();

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Token request failed: {tokenResponse.StatusCode}");
                    Console.WriteLine($"Response: {tokenJson}");
                    return null;
                }

                var tokenObject = JObject.Parse(tokenJson);
                var token = tokenObject["access_token"].ToString();

                return token;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception getting token: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Build email payload using proper JSON serialization
        /// This fixes the issue with special characters and JSON escaping
        /// </summary>
        private string BuildEmailPayload(
            MailModel mail)
        {
            // Build recipient list
            var toRecipients = new List<object>();

            if (!string.IsNullOrEmpty(mail.ToRecipients))
            {
                var toMails = mail.ToRecipients.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var tomail in toMails)
                {
                    toRecipients.Add(new
                    {
                        emailAddress = new { address = tomail.Trim() }
                    });
                }
            }

            // Build CC recipients
            var ccRecipients = new List<object>();
            if (!string.IsNullOrEmpty(mail.CcRecipients))
            {
                var ccEmails = mail.CcRecipients.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var cc in ccEmails)
                {
                    ccRecipients.Add(new
                    {
                        emailAddress = new { address = cc.Trim() }
                    });
                }
            }

            // Build BCC recipients
            var bccRecipients = new List<object>();
            if (!string.IsNullOrEmpty(mail.BccRecipients))
            {
                var bccEmails = mail.BccRecipients.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var bcc in bccEmails)
                {
                    bccRecipients.Add(new
                    {
                        emailAddress = new { address = bcc.Trim() }
                    });
                }
            }

            // Build attachments (if any)
            List<object>? attachments = null;
            if (mail.Attachments != null && mail.Attachments.Any())
            {
                attachments = new List<object>();
                foreach (var attachment in mail.Attachments)
                {
                    var fileAttachment = new Dictionary<string, object>
                    {
                        ["@oData.type"] = "#microsoft.graph.fileAttachment",
                        ["name"] = attachment.Name,
                        ["contentType"] = attachment.ContentType ?? "application/octet-stream",
                        ["contentBytes"] = attachment.ContentBytes // Base64 encoded string
                    };

                    attachments.Add(fileAttachment);
                }
            }

            // Build the message object
            var message = new
            {
                subject = mail.Subject,
                body = new
                {
                    contentType = "HTML",
                    content = mail.HtmlBody
                },
                toRecipients = toRecipients,
                ccRecipients = ccRecipients.Count > 0 ? ccRecipients : null,
                bccRecipients = bccRecipients.Count > 0 ? bccRecipients : null,
                importance = "high",
                attachments = attachments

            };

            var payload = new
            {
                message = message,
                saveToSentItems = true
            };

            // Use JsonConvert for proper serialization
            string jsonPayload = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            });

            Console.WriteLine($"📧 Email Payload:\n{jsonPayload}");

            return jsonPayload;
        }
    }
}
