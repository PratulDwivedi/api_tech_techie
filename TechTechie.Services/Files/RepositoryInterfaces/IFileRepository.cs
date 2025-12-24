using TechTechie.Services.Common.Models;
using TechTechie.Services.Files.Models;
using TechTechie.Services.Users.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Services.Files.RepositoryInterfaces
{
    public interface IFileRepository
    {
        public Task<ResponseMessageModel> SaveFilesInfo(List<FileModel> filesInfo, Dictionary<string, object> Data, SignedUser signedUser);

        public Task<FileModel> GetFileInfo(string fileId, SignedUser signedUser);

        public Task<StorageCredentialModel> GetStorageCredential(string credential_id, SignedUser signedUser);

    }
}
