using Dapper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Npgsql;
using System.ComponentModel;
using System.Net;
using System.Net.Mail;
using TechTechie.Services.Common.Models;
using TechTechie.Services.Files.Models;
using TechTechie.Services.Tenants.RepositoryInterfaces;
using TechTechie.Services.Users.Models;

namespace TechTechie.PostgresRepository.Tenants.Repos
{
    public class TenantRepository : ITenantRepository
    {
        private readonly TenantDbHelper _tenantDbHelper;
        private readonly IConfiguration _configuration;
        private readonly string _npgSqlconn;

        public TenantRepository(TenantDbHelper tenantDbHelper, IConfiguration configuration)
        {
            _tenantDbHelper = tenantDbHelper;
            _configuration = configuration;
            _npgSqlconn = _configuration.GetConnectionString("NpgSqlconn")!;
        }

        public Task<StorageCredentialModel> GetCredential(string credential_id)
        {
            throw new NotImplementedException();
        }

        public async Task<List<Dictionary<string, object>>> GetTenantAuthTypes(string tenant_code)
        {
            var sql = "SELECT fn_get_tenant_auth_types_url(@tenant_code) AS result";

            await using var masterConn = new NpgsqlConnection(_npgSqlconn);
            await masterConn.OpenAsync();

            var jsonResult = await masterConn.QueryFirstOrDefaultAsync<string>(sql, new { tenant_code });

            if (string.IsNullOrWhiteSpace(jsonResult))
                return new List<Dictionary<string, object>>();

            // Deserialize JSON into list of dictionaries
            return JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonResult)
                   ?? new List<Dictionary<string, object>>();
        }
        private async Task<EmailSettingModel> GetEmailSettingAsync(SignedUser signedUser)
        {
            string sql = "SELECT fn_get_enabled_email_setting(@tenant_id) as result";

            await using var masterConn = new NpgsqlConnection(_npgSqlconn);
            await masterConn.OpenAsync();

            var jsonResult = await masterConn.QueryFirstOrDefaultAsync<string>(sql, new { signedUser.tenant_id });

            if (string.IsNullOrWhiteSpace(jsonResult))
                throw new Exception("Email settings not found.");

            // Deserialize JSON into EmailSetting object    
            var emailSetting = JsonConvert.DeserializeObject<EmailSettingModel>(jsonResult);
            if (emailSetting == null)
                throw new Exception("Failed to deserialize Email settings.");

            return emailSetting;
        }

        private async Task<EmailLogModel> GetEmailLogAsync(int id, SignedUser signedUser)
        {
            string sql = "SELECT fn_get_email_log(@id, @tenant_id as result";

            await using var masterConn = new NpgsqlConnection(_npgSqlconn);
            await masterConn.OpenAsync();

            var jsonResult = await masterConn.QueryFirstOrDefaultAsync<string>(sql, new
            {
                id = id,
                tenant_id = signedUser.tenant_id
            }
            );

            if (string.IsNullOrWhiteSpace(jsonResult))
                throw new Exception("Email Log not found.");

            // Deserialize JSON into EmailLog object    
            var emailLog = JsonConvert.DeserializeObject<EmailLogModel>(jsonResult);
            if (emailLog == null)
                throw new Exception("Failed to deserialize Email log.");

            return emailLog;
        }
        public async Task<ResponseMessageModel> SendTestMail(SignedUser signedUser)
        {
            ResponseMessageModel responseMessage = new();

            MailMessage mail = new MailMessage();

            try
            {

                var EmailSetting = await GetEmailSettingAsync(signedUser);

                string EmailTo = EmailSetting.from_email;

                string EmailSubject = "Test Mail";
                string EmailBody = "PCS Infinity Test mail";

                mail.From = new MailAddress(EmailSetting.from_email, EmailSetting.from_name);
                mail.Subject = EmailSubject;
                mail.Body = EmailBody;
                mail.IsBodyHtml = true;
                mail.Priority = MailPriority.Normal;
                mail.To.Add(EmailTo);

                if (!string.IsNullOrEmpty(EmailSetting.email_bcc))
                    StringHelper.EmailAddress(EmailSetting.email_bcc).ForEach(email => mail.Bcc.Add(email));

                SmtpClient _SmtpClient = new SmtpClient();
                {
                    _SmtpClient.UseDefaultCredentials = false;
                    _SmtpClient.Host = EmailSetting.host;
                    _SmtpClient.Port = EmailSetting.port;
                    _SmtpClient.EnableSsl = true;
                    _SmtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    _SmtpClient.Credentials = new NetworkCredential(EmailSetting.from_id, EmailSetting.password);

                    _SmtpClient.Send(mail);

                    responseMessage.IsSuccess = true;
                    responseMessage.StatusCode = 200;
                    responseMessage.Message = "Mail configuration is valid.";

                }
            }
            catch (Exception ex)
            {
                responseMessage.IsSuccess = false;
                responseMessage.StatusCode = 400;
                responseMessage.Message = ex.ToString();
            }

            return responseMessage;
        }

        public async Task<ResponseMessageModel> SendMail(int id, SignedUser signedUser)
        {

            ResponseMessageModel response = new() { Message = "Email has been sent.", IsSuccess = true, StatusCode = 200 };

            MailMessage mail = new MailMessage();

            try
            {

                var EmailSetting = await GetEmailSettingAsync(signedUser);

                var emailLog = await GetEmailLogAsync(id, signedUser);


                mail.From = new MailAddress(EmailSetting.from_email, EmailSetting.from_name);

                if (!string.IsNullOrEmpty(emailLog.email_to))
                    StringHelper.EmailAddress(emailLog.email_to).ForEach(email => mail.To.Add(email));

                if (!string.IsNullOrEmpty(emailLog.email_cc))
                    StringHelper.EmailAddress(emailLog.email_cc).ForEach(email => mail.CC.Add(email));

                if (!string.IsNullOrEmpty(emailLog.email_bcc))
                    StringHelper.EmailAddress(emailLog.email_bcc).ForEach(email => mail.Bcc.Add(email));


                if (!string.IsNullOrEmpty(emailLog.attachment_hex))
                {
                    if (string.IsNullOrEmpty(emailLog.file_name))
                    {
                        emailLog.file_name = "attachment.pdf";
                    }

                    int NumberChars = emailLog.attachment_hex.Length;
                    byte[] bytes = new byte[NumberChars / 2];
                    for (int i = 0; i < NumberChars; i += 2)
                        bytes[i / 2] = Convert.ToByte(emailLog.attachment_hex.Substring(i, 2), 16);

                    MemoryStream pdf = new MemoryStream(bytes);
                    Attachment data = new Attachment(pdf, emailLog.file_name, StringHelper.GetMimeTypes(emailLog.file_name));
                    mail.Attachments.Add(data);
                }

                mail.Subject = emailLog.subject;
                mail.Body = emailLog.body;
                mail.IsBodyHtml = true;
                mail.Priority = MailPriority.Normal;


                SmtpClient _SmtpClient = new SmtpClient();
                {
                    _SmtpClient.UseDefaultCredentials = false;
                    _SmtpClient.Host = EmailSetting.host;
                    _SmtpClient.Port = EmailSetting.port;
                    _SmtpClient.EnableSsl = true;
                    _SmtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    _SmtpClient.Credentials = new NetworkCredential(EmailSetting.from_id, EmailSetting.password);

                    _SmtpClient.SendCompleted += (s, e) =>
                    {
                        _SmtpClient_SendCompleted(s, e, id, _npgSqlconn);
                        _SmtpClient.Dispose();
                    };

                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = 400;
                response.Message = ex.ToString();
            }
            return response;
        }

        static async void _SmtpClient_SendCompleted(object sender, AsyncCompletedEventArgs e, int id, string npgSqlconn)
        {
            string Message = "Email sent successfully.";
            if (e.Error != null)
            {
                Message = "0. " + e.Error.ToString();

            }
            if (e.Cancelled)
            {
                Message = "0. Cancelled.";
            }

            string sql = "SELECT fn_update_email_log_Message(@id , @messgae) as result";

            await using var masterConn = new NpgsqlConnection(npgSqlconn);
            await masterConn.OpenAsync();

            var jsonResult = await masterConn.QueryFirstOrDefaultAsync<string>(sql, new { id, Message });
        }

    }

}
