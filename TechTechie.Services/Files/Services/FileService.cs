using TechTechie.Services.Common.Models;
using TechTechie.Services.Files.Models;
using TechTechie.Services.Files.RepositoryInterfaces;
using TechTechie.Services.Files.ServiceInterfaces;
using TechTechie.Services.Users.Models;


namespace TechTechie.Services.Files.Services
{
    public class FileService : IFileService
    {
        private readonly IFileRepository _fileRepository;
        public FileService(IFileRepository fileRepository)
        {
            _fileRepository = fileRepository;
        }

        public Task<FileModel> GetFileInfo(string fileId, SignedUser signedUser)
        {
            return _fileRepository.GetFileInfo(fileId, signedUser);
        }

        public Task<StorageCredentialModel> GetStorageCredential(string credential_id, SignedUser signedUser)
        {
            return _fileRepository.GetStorageCredential(credential_id, signedUser);
        }

        public Task<ResponseMessageModel> SaveFilesInfo(List<FileModel> filesInfo, Dictionary<string, object> Data, SignedUser signedUser)
        {
            return _fileRepository.SaveFilesInfo(filesInfo, Data, signedUser);
        }
    }
}
