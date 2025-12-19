
namespace TechTechie.Services.Common.Models
{
    public class EmailSettingModel
    {
        public string host { get; set; }
        public string from_id { get; set; }
        public string from_email { get; set; }
        public string from_name { get; set; }
        public string password { get; set; }
        public int port { get; set; }
        public string? email_bcc { get; set; }
        public bool? is_smtp { get; set; }
        public Dictionary<string, object>? data { get; set; }
    }
}
