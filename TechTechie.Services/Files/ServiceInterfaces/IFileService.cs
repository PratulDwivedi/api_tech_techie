using TechTechie.Services.Common.Models;
using TechTechie.Services.Files.Models;
using TechTechie.Services.Users.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Services.Files.ServiceInterfaces
{
    public interface IFileService
    {
        public Task<ResponseMessageModel> SaveFilesInfo(List<FileModel> filesInfo, Dictionary<string, object> Data, SignedUser signedUser);

        public Task<FileModel> GetFileInfo(int fileId, SignedUser signedUser);

        public Task<StorageCredentialModel> GetStorageCredential(string credential_id, SignedUser signedUser);
    }
}
