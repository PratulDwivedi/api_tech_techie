using Azure.Storage.Blobs;
using TechTechie.Services.Files.Models;
using TechTechie.Services.Files.ServiceInterfaces;

namespace TechTechie.Services.Files.Services
{
    public class AzureBlobStorageService : IFileStorageService
    {

        public async Task<string> UploadAsync(FileUploadRequest file, StorageCredentialModel storageCredential)
        {
            var client = new BlobServiceClient(storageCredential.api_key);
            BlobContainerClient _containerClient = client.GetBlobContainerClient(storageCredential.Data!.folder_name);
            _containerClient.CreateIfNotExists();

            var blobClient = _containerClient.GetBlobClient(file.FileName);
            await blobClient.UploadAsync(file.Content, overwrite: true);
            return blobClient.Uri.ToString();
        }

        public async Task<Stream> DownloadAsync(string fileName, StorageCredentialModel storageCredential)
        {
            var client = new BlobServiceClient(storageCredential.api_key);
            BlobContainerClient _containerClient = client.GetBlobContainerClient(storageCredential.Data!.folder_name);
            var blobClient = _containerClient.GetBlobClient(fileName);
            var response = await blobClient.DownloadAsync();
            return response.Value.Content;
        }

        //public async Task DeleteAsync(string fileName, StorageCredentialModel storageCredential)
        //{
        //    var client = new BlobServiceClient(storageCredential.api_key);
        //    BlobContainerClient _containerClient = client.GetBlobContainerClient(storageCredential.Data!.folder_name);
        //    await _containerClient.DeleteBlobIfExistsAsync(fileName);
        //}
    }

}
