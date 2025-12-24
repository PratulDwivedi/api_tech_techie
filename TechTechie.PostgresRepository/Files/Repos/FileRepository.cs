using Newtonsoft.Json;
using TechTechie.Services.Common.Models;
using TechTechie.Services.Files.Models;
using TechTechie.Services.Files.RepositoryInterfaces;
using TechTechie.Services.Users.Models;

namespace TechTechie.PostgresRepository.Files.Repos
{
    public class FileRepository : IFileRepository
    {
        private readonly TenantDbHelper _tenantDbHelper;

        public FileRepository(TenantDbHelper tenantDbHelper)
        {
            _tenantDbHelper = tenantDbHelper;
        }

        public async Task<FileModel> GetFileInfo(string file_id, SignedUser signedUser)
        {
            FileModel fileInfo = new FileModel();

            var jsonString = await _tenantDbHelper.ExecuteTenantFunction(signedUser!,
                0,
                "SELECT fn_get_file_by_id(@file_id)",
            new
            {
                file_id
            });

            if (!string.IsNullOrEmpty(jsonString))
            {
                var data = JsonConvert.DeserializeObject<List<FileModel>>(jsonString)!;
                if (data != null || data!.Count > 0)
                {
                    fileInfo = data[0];
                }
            }
            return fileInfo;
        }

        public async Task<StorageCredentialModel> GetStorageCredential(string credential_id, SignedUser signedUser)
        {
            StorageCredentialModel credentialModel = new();
            var jsonString = await _tenantDbHelper.ExecuteTenantFunction(signedUser!,
                0,
                "SELECT fn_get_credential_tenants(@credential_id)",
            new
            {
                credential_id
            });

            if (!string.IsNullOrEmpty(jsonString))
            {
                var data = JsonConvert.DeserializeObject<List<StorageCredentialModel>>(jsonString)!;
                if (data != null || data!.Count > 0)
                {
                    credentialModel = data[0];
                }
            }
            return credentialModel;
        }

        public async Task<ResponseMessageModel> SaveFilesInfo(List<FileModel> filesInfo, Dictionary<string, object> data, SignedUser signedUser)
        {
            ResponseMessageModel response = new() { IsSuccess = true, Message = "file detail saved.", StatusCode = 200 };
            var dataJson = JsonConvert.SerializeObject(data);
            var files = JsonConvert.SerializeObject(filesInfo);

            var jsonString = await _tenantDbHelper.ExecuteTenantFunction(signedUser!,
                0,
                "SELECT fn_save_files(@p_files::jsonb, @p_data::jsonb )",
            new
            {
                p_files = files,
                p_data = dataJson
            });

            response.Data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonString);
            return response;
        }
    }
}
