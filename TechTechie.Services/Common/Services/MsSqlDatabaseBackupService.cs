using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechTechie.Services.Common.Models;

namespace TechTechie.Services.Common.Services
{
    public class MsSqlDatabaseBackupService : BackgroundService
    {
        private readonly ILogger<MsSqlDatabaseBackupService> _logger;
        private readonly DatabaseBackupOptions _options;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly string _logFilePath;
        private readonly bool _useFileLogging;
        private bool _compressionSupported;
        private DateTime? _lastBackupDate;

        public MsSqlDatabaseBackupService(ILogger<MsSqlDatabaseBackupService> logger, IOptions<DatabaseBackupOptions> options, IConfiguration configuration)
        {
            _compressionSupported = false;
            _logger = logger;
            _options = options.Value;
            _configuration = configuration;
            _logFilePath = _options.BackupBasePath;
            _connectionString = _configuration.GetConnectionString("sqlconn");
            _useFileLogging = !string.IsNullOrEmpty(_logFilePath);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            LogMessage("Database Backup Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var scheduledTime = now.Date.Add(_options.BackupTime);

                if (now > scheduledTime)
                {
                    scheduledTime = scheduledTime.AddDays(1);
                }

                var delay = scheduledTime - now;
                LogMessage($"Next backup scheduled at: {scheduledTime}");

                try
                {
                    await Task.Delay(delay, stoppingToken);

                    if (!stoppingToken.IsCancellationRequested)
                    {
                        var today = DateTime.Now.Date;

                        // Check if backup was already done today
                        if (_lastBackupDate.HasValue && _lastBackupDate.Value.Date == today)
                        {
                            LogMessage($"Backup already completed today ({today.ToString("yyyy-MM-dd")}), skipping");
                        }
                        else if (await IsBackupAlreadyDoneAsync(today, stoppingToken))
                        {
                            // Fix for CS1039 and CS1003: Correcting the unterminated string literal and ensuring proper syntax
                            LogMessage($"Backup folder already exists for today ({today.ToString("yyyy-MM-dd")})");

                            _lastBackupDate = today;
                        }
                        else
                        {
                            await PerformBackupAsync(stoppingToken);
                            _lastBackupDate = today;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    LogMessage("Backup service is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error in backup scheduling loop- {ex.Message}");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }
        private async Task<bool> IsBackupAlreadyDoneAsync(DateTime date, CancellationToken cancellationToken)
        {
            await Task.CompletedTask; // Make async for consistency

            var dateFolder = date.ToString("yyyy-MM-dd");
            var backupPath = Path.Combine(_options.BackupBasePath, dateFolder);

            // Check if folder exists and contains backup files
            if (Directory.Exists(backupPath))
            {
                var backupFiles = Directory.GetFiles(backupPath, "*.bak");
                if (backupFiles.Length > 0)
                {
                    _logger.LogInformation("Found {Count} existing backup files in {Path}", backupFiles.Length, backupPath);
                    return true;
                }
            }

            return false;
        }

        private async Task PerformBackupAsync(CancellationToken cancellationToken)
        {
            LogMessage("Starting database backup process");

            try
            {
                // Ensure base backup directory exists
                if (!Directory.Exists(_options.BackupBasePath))
                {
                    Directory.CreateDirectory(_options.BackupBasePath);
                    _logger.LogInformation("Created base backup directory: {Path}", _options.BackupBasePath);
                }

                // Create date-specific subfolder
                var dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
                var backupPath = Path.Combine(_options.BackupBasePath, dateFolder);

                if (!Directory.Exists(backupPath))
                {
                    Directory.CreateDirectory(backupPath);
                    LogMessage($"Created backup directory: {backupPath}");
                }

                var databases = await GetUserDatabasesAsync(cancellationToken);
                LogMessage($"Next backup scheduled at: {databases.Count}");

                foreach (var dbName in databases)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    await BackupDatabaseAsync(dbName, backupPath, cancellationToken);
                }

                LogMessage("Database backup process completed successfully");
            }
            catch (Exception ex)
            {
                LogMessage($"Next backup scheduled at: {"Failed to perform database backup"}");
            }
        }

        private async Task<List<string>> GetUserDatabasesAsync(CancellationToken cancellationToken)
        {
            var databases = new List<string>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var query = @"
            SELECT name 
            FROM sys.databases 
            WHERE name NOT IN ('master', 'tempdb', 'model', 'msdb')
            AND state_desc = 'ONLINE'
            ORDER BY name";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                databases.Add(reader.GetString(0));
            }

            return databases;
        }

        private async Task BackupDatabaseAsync(string databaseName, string backupPath, CancellationToken cancellationToken)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("HHmmss");
                var backupFileName = $"{databaseName}_{timestamp}.bak";
                var fullBackupPath = Path.Combine(backupPath, backupFileName);

                LogMessage($"Next backup scheduled at: {databaseName}");

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                string backupQuery = string.Empty;

                if (_compressionSupported)
                {
                    backupQuery = $@"
                    BACKUP DATABASE [{databaseName}] 
                    TO DISK = @BackupPath 
                    WITH FORMAT, 
                         INIT, 
                         NAME = @BackupName, 
                         COMPRESSION,
                         STATS = 10";
                }
                else
                {
                    backupQuery = $@"
                        BACKUP DATABASE [{databaseName}] 
                        TO DISK = @BackupPath 
                        WITH FORMAT, 
                             INIT, 
                             NAME = @BackupName, 
                             STATS = 10";


                }

                using var command = new SqlCommand(backupQuery, connection);
                command.CommandTimeout = 3600; // 1 hour timeout for large databases
                command.Parameters.AddWithValue("@BackupPath", fullBackupPath);
                command.Parameters.AddWithValue("@BackupName", $"{databaseName} Full Backup");

                await command.ExecuteNonQueryAsync(cancellationToken);

                var fileInfo = new FileInfo(fullBackupPath);
                var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);

                LogMessage($"Successfully backed up database: {databaseName}, Size: {fileSizeMB:F2} MB");
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to backup database: {databaseName} - {ex.Message}");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            LogMessage("Database Backup Service is stopping");
            await base.StopAsync(cancellationToken);
        }

        private void LogMessage(string message)
        {
            try
            {

                if (_useFileLogging)
                {
                    // Ensure base backup directory exists
                    if (!Directory.Exists(_options.BackupBasePath))
                    {
                        Directory.CreateDirectory(_options.BackupBasePath);

                    }

                    string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";

                    File.AppendAllText(_logFilePath + "/log.txt", logMessage + Environment.NewLine);

                }
                else
                {
                    // Fallback to console logging if file logging is not enabled
                    _logger.LogInformation($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}");
                }

                Console.WriteLine(message);
            }
            catch { }

        }
    }
}
