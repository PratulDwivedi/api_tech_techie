using TechTechie.Services.Files.Models;
using TechTechie.Services.Files.ServiceInterfaces;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Services.Files.Services
{
    public class LocalStorageService : IFileStorageService
    {

        public async Task<string> UploadAsync(FileUploadRequest file, StorageCredentialModel storageCredential)
        {
            if (file == null || file.Content == null || string.IsNullOrWhiteSpace(file.FileName))
                throw new ArgumentException("Invalid file upload request.");

            // Ensure directory exists
            if (!Directory.Exists(storageCredential.Data!.folder_name))
                Directory.CreateDirectory(storageCredential.Data.folder_name!);

            // Combine full path
            string filePath = Path.Combine(storageCredential.Data.folder_name!, file.FileName);

            // Will create the directory if it does not exist and file where code is running
            await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await file.Content.CopyToAsync(stream);

            return file.FileName;
        }

        public async Task<Stream> DownloadAsync(string fileName, StorageCredentialModel storageCredential)
        {
            if (storageCredential?.Data?.folder_name == null)
                throw new ArgumentException("Invalid storage credential or folder name.");

            // Combine full path
            string filePath = Path.Combine(storageCredential.Data.folder_name, fileName);

            return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

    }

}
