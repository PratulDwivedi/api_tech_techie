using TechTechie.Services.Files.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Services.Files.ServiceInterfaces
{
    public interface IFileStorageService
    {
        Task<string> UploadAsync(FileUploadRequest file, StorageCredentialModel storageCredential);
        Task<Stream> DownloadAsync(string fileName, StorageCredentialModel storageCredential);
        
    }

}
