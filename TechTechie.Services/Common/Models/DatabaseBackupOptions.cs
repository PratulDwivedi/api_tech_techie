namespace TechTechie.Services.Common.Models
{
    public class DatabaseBackupOptions
    {
        public bool IsEnabled { get; set; } = false;
        public string BackupBasePath { get; set; } = string.Empty;
        public TimeSpan BackupTime { get; set; } = new TimeSpan(0, 0, 0); // Midnight
    }
}
