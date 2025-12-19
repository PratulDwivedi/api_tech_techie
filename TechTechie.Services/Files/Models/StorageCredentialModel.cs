using TechTechie.Services.Common.Models;


namespace TechTechie.Services.Files.Models
{
    public class StorageCredentialModel : TenantCredentialModel
    {
        public Storage? Data { get; set; }
        public Dictionary<string, object>? meta { get; set; }
    }

    public class Storage
    {
        public string? folder_name { get; set; }
        public string? container_name { get; set; }
        public string? exclude_file_extension { get; set; }
    }
}
