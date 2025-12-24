using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTechie.Services.Common.Models;
using TechTechie.Services.Dynamic.ServiceInterfaces;
using TechTechie.Services.Files.Models;
using TechTechie.Services.Files.ServiceInterfaces;
using TechTechie.Services.Tenant.ServiceInterfaces;
using TechTechie.Services.Users.Models;
using TechTechie.WebApi.Helpers;
using Storage = TechTechie.Services.Files.Models.Storage;

namespace TechTechie.WebApi.Controllers
{
    [Authorize(Policy = "ApiPolicy")]
    [ApiController]
    [Route("api")]
    public class FileController : ControllerBase
    {

        private readonly IConfiguration _config;
        private readonly IFileService _fileService;
        private readonly IWebHostEnvironment _environment;
        private readonly IDynamicService _dynamicService;
        private readonly ITenantService _tenantService;
        private readonly IStorageServiceFactory _factory;
        private readonly bool isShowError;

        public FileController(IConfiguration config, IFileService fileService,
            IWebHostEnvironment environment, IDynamicService dynamicService, ITenantService tenantService, IStorageServiceFactory factory)
        {
            _config = config;
            _fileService = fileService;
            _environment = environment;
            _dynamicService = dynamicService;
            _tenantService = tenantService;
            _factory = factory;

            if (_config["AppSettings:ShowError"] == "No")
                isShowError = false;
            else
                isShowError = true;

        }

        [HttpPost("file")]
        [HttpPost("file/{id}")]
        public async Task<ActionResult> UploadFile(List<IFormFile> files, string? id)
        {
            ResponseMessageModel response = new();
            SignedUser signedUser = HttpHelper.GetSignedUser(HttpContext);

            StorageCredentialModel storageCredential = await GetStorageCredential(signedUser);

            Dictionary<string, object> Data = HttpHelper.GetQueryStringData(HttpContext.Request);

            try
            {
                long size = files.Sum(f => f.Length);

                List<FileModel> lstAttach = new();
                string guid;
                if (string.IsNullOrEmpty(id))
                    guid = Guid.NewGuid().ToString("N");
                else
                    guid = id;

                string[] excludedExtensions;

                string excludeFileExtension = storageCredential!.Data!.exclude_file_extension!;

                if (!string.IsNullOrEmpty(excludeFileExtension))
                {
                    excludedExtensions = excludeFileExtension.Split(",");
                }
                else
                {
                    excludedExtensions = Array.Empty<string>();
                }

                foreach (var file in files)
                {
                    // Check the file extension
                    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                    if (excludedExtensions.Contains(fileExtension))
                    {
                        throw new Exception("File format is not allowed");
                    }
                    if (file.Length > 0)
                    {
                        var service = _factory.Get(storageCredential.credential_id!);

                        var fileModel = GetFileModel(file, guid);

                        var result = await service.UploadAsync(new FileUploadRequest
                        {
                            FileName = fileModel.path,
                            ContentType = file.ContentType,
                            Content = file.OpenReadStream()
                        }, storageCredential);

                        if (!string.IsNullOrEmpty(result))
                        {
                            lstAttach.Add(fileModel);
                        }
                    }
                }

                if (lstAttach.Count > 0)
                {
                    response = await _fileService.SaveFilesInfo(lstAttach, Data, signedUser);
                }
                else
                {
                    response = new ResponseMessageModel() { IsSuccess = false, StatusCode = 400, Message = "Unable to upload file." };
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                response.StatusCode = 400;

            }
            //var logResult = await _dynamicService.SaveApiEventLog(new ApiEventLogModel()
            //{
            //    EventType = "UploadFile",
            //    ResponseData = response.Data,
            //    IsSuccess = response.isSuccess,
            //    Message = response.Message
            //}, authUser);

            //if (!response.IsSuccess)
            //{
            //    response.Message = "Unable to perform the action , please refer api event log.";
            //}
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        [HttpGet("file/{id}")]
        [HttpGet("file/{id}/{format}")]
        public async Task<IActionResult> Download(string id, string? format, [FromQuery] int width = 0, [FromQuery] int height = 0)
        {

            StorageCredentialModel storageCredential = new();
            FileModel fileInfo = new();
            try
            {

                var signedUser = HttpHelper.GetSignedUser(HttpContext);

                fileInfo = await _fileService.GetFileInfo(id, signedUser);

                if (fileInfo.id == null || fileInfo.id == 0)
                    return NotFound(new ResponseMessageModel { IsSuccess = false, Message = "File not found in record.", StatusCode = 404 });

                storageCredential = await GetStorageCredential(signedUser);

                var storageProvider = storageCredential.credential_id;

                var service = _factory.Get(storageCredential.credential_id!);
                var stream = await service.DownloadAsync(fileInfo.path!, storageCredential);
                return File(stream, HttpHelper.GetContentType(fileInfo.path!), fileInfo.name);

            }
            catch (HttpRequestException ex)
            {
                string Message = "";

                if (isShowError)
                {
                    Message = $"Http request failed with Message: {ex.Message}";
                }
                else
                {
                    Message = "Something went wrong. We have already been notified and we are working on the fix.";
                }
                return Ok(new ResponseMessageModel { IsSuccess = false, Message = Message, StatusCode = 500, Data = fileInfo });
            }
            catch (Exception ex)
            {
                string Message = "";

                if (isShowError)
                {
                    Message = $"Issue in file downloaing from storage. {storageCredential.credential_id} , {ex.Message}";
                }
                else
                {
                    Message = "Something went wrong. We have already been notified and we are working on the fix.";

                }

                return Ok(new ResponseMessageModel { IsSuccess = false, Message = Message, StatusCode = 404, Data = fileInfo });

            }
        }

        private FileModel GetFileModel(IFormFile file, string guid)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var extension = Path.GetExtension(file.FileName);
            var baseName = Path.GetFileNameWithoutExtension(file.FileName);

            var newFileName = $"{baseName}_{timestamp}{extension}";

            FileModel fileModel = new()
            {
                name = file.FileName,
                path = newFileName,
                file_group_id = guid,
                size = file.Length
            };
            return fileModel;
        }

        private async Task<StorageCredentialModel> GetStorageCredential(SignedUser signedUser)
        {
            StorageCredentialModel storageCredential = new();

            try
            {
                string storageProvider = _config["FileStorage:Provider"]!; //"microsoft-azure-blob"


                storageCredential = await _fileService.GetStorageCredential(storageProvider, signedUser);

                if (storageCredential.api_url == null)
                {
                    storageCredential.api_url = _config["FileStorage:ApiUrl"]!;
                }

                if (storageCredential.api_key == null)
                {
                    storageCredential.api_key = _config["FileStorage:ConnectionString"]!;
                }

                if (storageCredential.Data == null)
                {
                    storageCredential.Data = new Storage()
                    {
                        exclude_file_extension = _config["FileStorage:ExcludeFileExtension"]!,
                        container_name = _config["FileStorage:ContainerName"]!,
                        folder_name = signedUser.tenant_code!.ToLower()
                    };
                }

                // in case of tenant credential not found in Database
                if (string.IsNullOrEmpty(storageCredential!.credential_id))
                {
                    storageCredential!.credential_id = storageProvider;
                }

                if (string.IsNullOrEmpty(storageCredential!.Data!.container_name))
                {
                    storageCredential.Data!.container_name = _config["FileStorage:ContainerName"]!;
                }

                if (string.IsNullOrEmpty(storageCredential!.Data!.folder_name))
                {
                    storageCredential.Data!.folder_name = signedUser.tenant_code!.ToLower();
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Unable to get storage credential. " + ex.Message);
            }
            return storageCredential;
        }
    }
}
