
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;

namespace TechTechie.MsSqlToSupabase
{
    class Program
    {
        // Configuration
        private const string SQL_CONNECTION_STRING = "Data Source=10.0.0.5,1433;Initial Catalog=FAI-SEMINAR-DB;User ID=sa;Password=Dreamer@#2023;Connection Timeout=30;TrustServerCertificate=True";
        //private const string SUPABASE_CONNECTION_STRING = "Host=db.tpgyuqvncljnuyrohqre.supabase.co;Port=6543;Database=postgres;Username=postgres;Password=FZK@kR@3Rz9k@q;Ssl Mode=Require;Trust Server Certificate=true;Timeout=30;Command Timeout=300;";
        // Transaction pooler connection string
        private const string SUPABASE_CONNECTION_STRING = "User Id=postgres.tpgyuqvncljnuyrohqre;Password=FZK@kR@3Rz9k@q;Server=aws-0-ap-south-1.pooler.supabase.com;Port=6543;Database=postgres";
        private const string SOURCE_SCHEMA = "EVT";
        private const string TARGET_SCHEMA = "seminar";
        private const int BATCH_SIZE = 1000;

        private const string MIGRATION_LOG_FILE = "migration_log.json";

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== EVT to Supabase Migration Tool ===");
            Console.WriteLine($"Started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");

            try
            {
                // Load migration log
                var migrationLog = LoadMigrationLog();

                // Get list of tables to migrate
                var tables = await GetSourceTables();
                Console.WriteLine($"Found {tables.Count} tables in {SOURCE_SCHEMA} schema\n");

                var summary = new MigrationSummary
                {
                    StartTime = DateTime.Now,
                    SuccessfulTables = new List<string>(),
                    FailedTables = new List<string>(),
                    SkippedTables = new List<string>()
                };

                foreach (var table in tables)
                {
                    //if (table != "Sponsor")
                    //{
                    //    continue;
                    //}
                    var targetTableName = StringHelper.GetTargetTableName(table);

                    Console.WriteLine($"Processing table: {table} -> {targetTableName}");

                    // Check if already migrated
                    if (migrationLog.ContainsKey(table) && migrationLog[table].Status == "Success")
                    {
                        Console.WriteLine($"  ℹ Already migrated on {migrationLog[table].MigratedAt:yyyy-MM-dd HH:mm:ss}");
                        Console.WriteLine($"  Would you like to re-migrate? (y/n)");

                        // For automated runs, skip already migrated tables
                        Console.WriteLine($"  Skipping (already migrated)\n");
                        summary.SkippedTables.Add(table);
                        continue;
                    }

                    try
                    {
                        await MigrateTable(table, targetTableName);
                        Console.WriteLine($"✓ Completed: {table}\n");

                        summary.SuccessfulTables.Add(table);
                        migrationLog[table] = new MigrationLogEntry
                        {
                            Status = "Success",
                            MigratedAt = DateTime.Now,
                            TargetTable = targetTableName
                        };

                        SaveMigrationLog(migrationLog);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Failed: {table}");
                        Console.WriteLine($"  Error: {ex.Message}\n");

                        summary.FailedTables.Add(table);
                        migrationLog[table] = new MigrationLogEntry
                        {
                            Status = "Failed",
                            MigratedAt = DateTime.Now,
                            TargetTable = targetTableName,
                            ErrorMessage = ex.Message
                        };

                        SaveMigrationLog(migrationLog);
                    }
                }

                summary.EndTime = DateTime.Now;
                StringHelper.PrintSummary(summary);

                Console.WriteLine($"\nMigration log saved to: {MIGRATION_LOG_FILE}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static Dictionary<string, MigrationLogEntry> LoadMigrationLog()
        {
            if (!File.Exists(MIGRATION_LOG_FILE))
            {
                return new Dictionary<string, MigrationLogEntry>();
            }

            try
            {
                var json = File.ReadAllText(MIGRATION_LOG_FILE);
                return JsonSerializer.Deserialize<Dictionary<string, MigrationLogEntry>>(json)
                    ?? new Dictionary<string, MigrationLogEntry>();
            }
            catch
            {
                Console.WriteLine("Warning: Could not load migration log, starting fresh");
                return new Dictionary<string, MigrationLogEntry>();
            }
        }

        static void SaveMigrationLog(Dictionary<string, MigrationLogEntry> log)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(log, options);
            File.WriteAllText(MIGRATION_LOG_FILE, json);
        }

        static async Task<List<string>> GetSourceTables()
        {
            var tables = new List<string>();

            using (var conn = new SqlConnection(SQL_CONNECTION_STRING))
            {
                await conn.OpenAsync();
                var query = @"
                    SELECT TABLE_NAME 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_SCHEMA = @Schema 
                    AND TABLE_TYPE = 'BASE TABLE'
                    ORDER BY TABLE_NAME";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Schema", SOURCE_SCHEMA);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            tables.Add(reader.GetString(0));
                        }
                    }
                }
            }

            return tables;
        }

        static async Task MigrateTable(string tableName, string targetTableName)
        {
            Console.WriteLine($"  Source: {SOURCE_SCHEMA}.{tableName}");
            Console.WriteLine($"  Target: {TARGET_SCHEMA}.{targetTableName}");

            // Get source columns
            var sourceColumns = await GetSourceColumns(tableName);
            Console.WriteLine($"  Source columns: {sourceColumns.Count}");

            // Get primary key column
            var primaryKeyColumn = await GetPrimaryKeyColumn(tableName);
            Console.WriteLine($"  Primary key: {primaryKeyColumn ?? "None"}");

            // Get target columns
            var targetColumns = await GetTargetColumns(targetTableName);
            Console.WriteLine($"  Target columns: {targetColumns.Count}");

            // Map columns
            var columnMapping = MapColumns(sourceColumns, targetColumns);
            Console.WriteLine($"  Mapped columns: {columnMapping.Count}");

            if (columnMapping.Count == 0)
            {
                throw new Exception("No columns mapped");
            }

            // Get total count
            var totalRows = await GetRowCount(tableName);
            Console.WriteLine($"  Total rows: {totalRows}");

            if (totalRows == 0)
            {
                Console.WriteLine("  No data to migrate");
                return;
            }

            // Check for existing data to avoid duplicates
            var existingIds = await GetExistingIds(targetTableName, primaryKeyColumn);
            Console.WriteLine($"  Existing records in target: {existingIds.Count}");

            // Migrate data in batches
            await MigrateData(tableName, targetTableName, columnMapping, totalRows, primaryKeyColumn, existingIds);
        }

        static async Task<string> GetPrimaryKeyColumn(string tableName)
        {
            using (var conn = new SqlConnection(SQL_CONNECTION_STRING))
            {
                await conn.OpenAsync();
                var query = @"
                    SELECT COLUMN_NAME
                    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                    WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1
                    AND TABLE_SCHEMA = @Schema
                    AND TABLE_NAME = @TableName";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Schema", SOURCE_SCHEMA);
                    cmd.Parameters.AddWithValue("@TableName", tableName);

                    var result = await cmd.ExecuteScalarAsync();
                    return result?.ToString();
                }
            }
        }

        static async Task<HashSet<object>> GetExistingIds(string tableName, string primaryKeyColumn)
        {
            var ids = new HashSet<object>();

            if (string.IsNullOrEmpty(primaryKeyColumn))
                return ids;

            var targetPkColumn = StringHelper.ColumnMappings.ContainsKey(primaryKeyColumn)
                ? StringHelper.ColumnMappings[primaryKeyColumn]
                : StringHelper.ToSnakeCase(primaryKeyColumn);

            try
            {
                using (var conn = new NpgsqlConnection(SUPABASE_CONNECTION_STRING))
                {
                    await conn.OpenAsync();
                    var query = $"SELECT {targetPkColumn} FROM {TARGET_SCHEMA}.{tableName}";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var value = reader.GetValue(0);
                                if (value != null && value != DBNull.Value)
                                {
                                    ids.Add(value);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Target table might not exist or column name different
            }

            return ids;
        }

        static async Task<List<ColumnInfo>> GetSourceColumns(string tableName)
        {
            var columns = new List<ColumnInfo>();

            using (var conn = new SqlConnection(SQL_CONNECTION_STRING))
            {
                await conn.OpenAsync();
                var query = @"
                    SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = @Schema AND TABLE_NAME = @TableName
                    ORDER BY ORDINAL_POSITION";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Schema", SOURCE_SCHEMA);
                    cmd.Parameters.AddWithValue("@TableName", tableName);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            columns.Add(new ColumnInfo
                            {
                                Name = reader.GetString(0),
                                DataType = reader.GetString(1),
                                IsNullable = reader.GetString(2) == "YES"
                            });
                        }
                    }
                }
            }

            return columns;
        }

        static async Task<List<string>> GetTargetColumns(string tableName)
        {
            var columns = new List<string>();

            using (var conn = new NpgsqlConnection(SUPABASE_CONNECTION_STRING))
            {
                try
                {
                    await conn.OpenAsync();
                    var query = @"
                    SELECT column_name
                    FROM information_schema.columns
                    WHERE table_schema = @Schema AND table_name = @TableName
                    ORDER BY ordinal_position";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("Schema", TARGET_SCHEMA);
                        cmd.Parameters.AddWithValue("TableName", tableName);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                columns.Add(reader.GetString(0));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    string message = ex.Message;
                }

            }

            return columns;
        }

        static Dictionary<string, string> MapColumns(List<ColumnInfo> sourceColumns, List<string> targetColumns)
        {
            var mapping = new Dictionary<string, string>();

            try
            {
                foreach (var sourceCol in sourceColumns)
                {
                    // Check predefined mappings first
                    if (StringHelper.ColumnMappings.TryGetValue(sourceCol.Name, out var mappedName))
                    {
                        if (targetColumns.Contains(mappedName, StringComparer.OrdinalIgnoreCase))
                        {
                            mapping[sourceCol.Name] = mappedName;
                            continue;
                        }
                    }

                    // Try snake_case conversion
                    var snakeCaseName = StringHelper.ToSnakeCase(sourceCol.Name);
                    if (targetColumns.Contains(snakeCaseName, StringComparer.OrdinalIgnoreCase))
                    {
                        mapping[sourceCol.Name] = snakeCaseName;
                    }
                }
            }
            catch (Exception ex)
            {

                string message = ex.Message;
            }


            return mapping;
        }

        static async Task<int> GetRowCount(string tableName)
        {
            using (var conn = new SqlConnection(SQL_CONNECTION_STRING))
            {
                await conn.OpenAsync();
                var query = $"SELECT COUNT(*) FROM [{SOURCE_SCHEMA}].[{tableName}]";

                using (var cmd = new SqlCommand(query, conn))
                {
                    return (int)await cmd.ExecuteScalarAsync();
                }
            }
        }

        static async Task MigrateData(string sourceTable, string targetTable,
            Dictionary<string, string> columnMapping, int totalRows,
            string primaryKeyColumn, HashSet<object> existingIds)
        {
            var offset = 0;
            var migratedRows = 0;
            var skippedRows = 0;

            while (offset < totalRows)
            {
                var batch = await FetchBatch(sourceTable, columnMapping, offset, primaryKeyColumn);

                if (batch.Count > 0)
                {
                    var newRecords = batch.Where(row =>
                    {
                        if (string.IsNullOrEmpty(primaryKeyColumn) || !row.ContainsKey(primaryKeyColumn))
                            return true;

                        var pkValue = row[primaryKeyColumn];
                        return pkValue == null || !existingIds.Contains(pkValue);
                    }).ToList();

                    skippedRows += batch.Count - newRecords.Count;

                    if (newRecords.Count > 0)
                    {
                        await InsertBatch(targetTable, columnMapping, newRecords);
                        migratedRows += newRecords.Count;
                    }

                    var progress = ((offset + batch.Count) * 100) / totalRows;
                    Console.Write($"\r  Progress: {offset + batch.Count}/{totalRows} ({progress}%) | Migrated: {migratedRows} | Skipped: {skippedRows}");
                }

                offset += BATCH_SIZE;
            }

            Console.WriteLine();
        }

        static async Task<List<Dictionary<string, object>>> FetchBatch(string tableName,
            Dictionary<string, string> columnMapping, int offset, string primaryKeyColumn)
        {
            var data = new List<Dictionary<string, object>>();
            var sourceColumns = string.Join(", ", columnMapping.Keys.Select(c => $"[{c}]"));

            // Include primary key if not already in mapping
            if (!string.IsNullOrEmpty(primaryKeyColumn) && !columnMapping.ContainsKey(primaryKeyColumn))
            {
                sourceColumns += $", [{primaryKeyColumn}]";
            }

            using (var conn = new SqlConnection(SQL_CONNECTION_STRING))
            {
                await conn.OpenAsync();
                var query = $@"
                    SELECT {sourceColumns}
                    FROM [{SOURCE_SCHEMA}].[{tableName}]
                    ORDER BY (SELECT NULL)
                    OFFSET @Offset ROWS
                    FETCH NEXT @BatchSize ROWS ONLY";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Offset", offset);
                    cmd.Parameters.AddWithValue("@BatchSize", BATCH_SIZE);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();

                            foreach (var col in columnMapping.Keys)
                            {
                                var value = reader[col];

                                // Handle fixed value mappings (e.g., OrgId = 4)
                                if (StringHelper.FixedValueMappings.ContainsKey(col))
                                {
                                    row[col] = StringHelper.FixedValueMappings[col];
                                }
                                else
                                {
                                    row[col] = value == DBNull.Value ? null : value;
                                }
                            }

                            // Include primary key for duplicate detection
                            if (!string.IsNullOrEmpty(primaryKeyColumn) && !columnMapping.ContainsKey(primaryKeyColumn))
                            {
                                var pkValue = reader[primaryKeyColumn];
                                row[primaryKeyColumn] = pkValue == DBNull.Value ? null : pkValue;
                            }

                            data.Add(row);
                        }
                    }
                }
            }

            return data;
        }

        static async Task InsertBatch(
    string tableName,
    Dictionary<string, string> columnMapping,
    List<Dictionary<string, object>> data)
        {
            await using var conn = new NpgsqlConnection(SUPABASE_CONNECTION_STRING);
            await conn.OpenAsync();

            // 1️⃣ Load column types from Postgres (ONCE)
            var columnTypes = await LoadColumnTypes(conn, TARGET_SCHEMA, tableName);

            // 2️⃣ Build deterministic column list
            var columns = columnMapping
                .Select(m => new
                {
                    SourceKey = m.Key,      // key in input row
                    ColumnName = m.Value   // db column
                })
                .ToList();

            // 3️⃣ Build SQL with NAMED parameters
            var query = $@"
        INSERT INTO {TARGET_SCHEMA}.{tableName}
        ({string.Join(", ", columns.Select(c => c.ColumnName))})
        VALUES ({string.Join(", ", columns.Select(c => "@" + c.ColumnName))})
        ON CONFLICT (id) DO NOTHING";

            await using var cmd = new NpgsqlCommand(query, conn);

            // 4️⃣ Create parameters ONCE with FIXED types
            foreach (var col in columns)
            {
                if (!columnTypes.TryGetValue(col.ColumnName, out var dbType))
                    throw new InvalidOperationException(
                        $"Column '{col.ColumnName}' not found in schema metadata");

                cmd.Parameters.Add(new NpgsqlParameter
                {
                    ParameterName = col.ColumnName,
                    NpgsqlDbType = dbType,
                    Value = DBNull.Value
                });
            }

            // 5️⃣ Execute rows
            foreach (var row in data)
            {
                foreach (var col in columns)
                {
                    var raw = row.TryGetValue(col.SourceKey, out var v) ? v : null;



                    var param = cmd.Parameters[col.ColumnName];
                    var converted = StringHelper.ConvertToColumnType(raw, param.NpgsqlDbType);

                    param.Value = converted ?? DBNull.Value;

                }

                await cmd.ExecuteNonQueryAsync();
            }
        }
        static async Task<Dictionary<string, NpgsqlDbType>> LoadColumnTypes(
            NpgsqlConnection conn,
            string schema,
            string table)
        {
            const string sql = @"
        SELECT column_name, data_type, udt_name
        FROM information_schema.columns
        WHERE table_schema = @schema
          AND table_name   = @table";

            var result = new Dictionary<string, NpgsqlDbType>();

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("schema", schema);
            cmd.Parameters.AddWithValue("table", table);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var column = reader.GetString(0);
                var dataType = reader.GetString(1);
                var udt = reader.GetString(2);

                result[column] = StringHelper.MapPostgresType(dataType, udt);
            }

            return result;
        }

    }
}