namespace TechTechie.MsSqlToSupabase
{
    public class ColumnInfo
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool IsNullable { get; set; }
    }

    public class MigrationLogEntry
    {
        public string Status { get; set; }
        public DateTime MigratedAt { get; set; }
        public string TargetTable { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class MigrationSummary
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<string> SuccessfulTables { get; set; }
        public List<string> FailedTables { get; set; }
        public List<string> SkippedTables { get; set; }
    }
}
