using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using TechTechie.Services.Common.Models;
using TechTechie.Services.Tenant.ServiceInterfaces;
using TechTechie.Services.Tenants.Models;
using TechTechie.Services.Tenants.RepositoryInterfaces;
using TechTechie.Services.Users.Models;

namespace TechTechie.Services.Tenant.Services
{
    public class TenantService : ITenantService
    {
        private readonly IConfiguration _config;
        private readonly ITenantRepository _tenantRepository;

        public TenantService(IConfiguration config, ITenantRepository tenantRepository)
        {
            _config = config;
            _tenantRepository = tenantRepository;
        }

        public Task<DateTime> GetAppValidity()
        {
            string WebAppUrl = _config["AppSettings:WebAppUrl"]!;
            string AppKey = _config["AppSettings:AppKey"]!;

            if (!RegisteredTenants.Tenants.ContainsKey(WebAppUrl))
            {
                throw new Exception("Invalid tenant. Domain url is not registered : " + WebAppUrl);
            }

            if (string.IsNullOrEmpty(AppKey))
            {
                throw new Exception("App Key is not found in app settings.");
            }
            try
            {
                string appKeyValue = DecryptValue(AppKey);
            }
            catch (Exception)
            {
                throw new Exception("Invalid App Key in app settings.");
            }

            DateTime validTillDate = DateTime.Now.AddDays(30);
            // TODO: Add actual valiDataion logic here using tenant_code and appKeyValue

            return Task.FromResult(validTillDate); // Placeholder: replace with real logic
        }


        public string DecryptValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }
            EncryptDecruptModel encryptDecruptModel = new() { EncryptedValue = value };

            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(encryptDecruptModel.EncryptedValue);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(encryptDecruptModel.Key);
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }

        public string EncryptValue(EncryptDecruptModel encryptDecruptModel)
        {

            if (string.IsNullOrEmpty(encryptDecruptModel.Value))
            {
                return "";
            }

            byte[] iv = new byte[16];
            byte[] array;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(encryptDecruptModel.Key);
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))
                        {
                            streamWriter.Write(encryptDecruptModel.Value);
                        }

                        array = memoryStream.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(array);
        }

        public Task<List<Dictionary<string, object>>> GetTenantAuthTypes(string tenant_code)
        {
            return _tenantRepository.GetTenantAuthTypes(tenant_code);
        }

        public Task<ResponseMessageModel> SendTestMail(SignedUser signedUser)
        {
            return _tenantRepository.SendTestMail(signedUser);
        }

        public Task<ResponseMessageModel> SendMail(int id, SignedUser signedUser)
        {
            return _tenantRepository.SendMail(id, signedUser);
        }
    }
}
