using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.MsSqlToSupabase
{
    public static class StringHelper
    {
        public static string ToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var sb = new StringBuilder();
            sb.Append(char.ToLower(input[0]));

            for (int i = 1; i < input.Length; i++)
            {
                if (char.IsUpper(input[i]))
                {
                    sb.Append('_');
                    sb.Append(char.ToLower(input[i]));
                }
                else
                {
                    sb.Append(input[i]);
                }
            }

            return sb.ToString();
        }

        public static string ToPlural(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Handle common pluralization rules
            if (input.EndsWith("y") && input.Length > 1 && !"aeiou".Contains(input[input.Length - 2]))
            {
                return input.Substring(0, input.Length - 1) + "ies";
            }
            else if (input.EndsWith("s") || input.EndsWith("x") || input.EndsWith("z") ||
                     input.EndsWith("ch") || input.EndsWith("sh"))
            {
                return input + "es";
            }
            else
            {
                return input + "s";
            }
        }



        // Column mapping for common fields
        public static readonly Dictionary<string, string> ColumnMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Primary Keys
            { "Id", "id" },
            //{ "ID", "id" },
            
            // Tenant/Organization
            { "OrgId", "tenant_id" },
            //{ "OrgID", "tenant_id" },
            { "TenantId", "tenant_id" },
           // { "TenantID", "tenant_id" },
            { "OrganizationId", "tenant_id" },
            
            // Audit fields - Created
            { "CreatedBy", "created_by" },
            { "CreatedOn", "created_at" },
            { "CreatedDate", "created_at" },
            { "CreatedTime", "created_at" },
            
            // Audit fields - Modified
            { "ModifiedBy", "updated_by" },
            { "ModifiedOn", "updated_at" },
            { "ModifiedDate", "updated_at" },
            { "UpdatedBy", "updated_by" },
            { "UpdatedOn", "updated_at" },
            { "UpdatedDate", "updated_at" },
            
            // Status fields
            { "IsActive", "is_active" },
          
        };
        // Special handling columns that need fixed values
        public static readonly Dictionary<string, object> FixedValueMappings = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            { "OrgId", 4 },
            //{ "OrgID", 4 },
            { "TenantId", 4 },
            //{ "TenantID", 4 }
        };
        public static void PrintSummary(MigrationSummary summary)
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("MIGRATION SUMMARY");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine($"Started:  {summary.StartTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Finished: {summary.EndTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Duration: {(summary.EndTime - summary.StartTime).TotalMinutes:F2} minutes");
            Console.WriteLine();
            Console.WriteLine($"✓ Successful: {summary.SuccessfulTables.Count}");
            Console.WriteLine($"✗ Failed:     {summary.FailedTables.Count}");
            Console.WriteLine($"⊝ Skipped:    {summary.SkippedTables.Count}");
            Console.WriteLine(new string('=', 60));

            if (summary.SuccessfulTables.Count > 0)
            {
                Console.WriteLine("\nSuccessfully migrated tables:");
                foreach (var table in summary.SuccessfulTables)
                {
                    Console.WriteLine($"  ✓ {table}");
                }
            }

            if (summary.FailedTables.Count > 0)
            {
                Console.WriteLine("\nFailed tables:");
                foreach (var table in summary.FailedTables)
                {
                    Console.WriteLine($"  ✗ {table}");
                }
            }

            if (summary.SkippedTables.Count > 0)
            {
                Console.WriteLine("\nSkipped tables (already migrated):");
                foreach (var table in summary.SkippedTables)
                {
                    Console.WriteLine($"  ⊝ {table}");
                }
            }
        }

        public static NpgsqlDbType MapPostgresType(string dataType, string udtName)
        {
            return (dataType, udtName) switch
            {
                ("integer", _) => NpgsqlDbType.Integer,
                ("bigint", _) => NpgsqlDbType.Bigint,
                ("boolean", _) => NpgsqlDbType.Boolean,
                ("timestamp without time zone", _) => NpgsqlDbType.Timestamp,
                ("timestamp with time zone", _) => NpgsqlDbType.TimestampTz,
                ("text", _) => NpgsqlDbType.Text,
                ("character varying", _) => NpgsqlDbType.Varchar,
                ("uuid", _) => NpgsqlDbType.Uuid,
                ("jsonb", _) => NpgsqlDbType.Jsonb,
                _ => NpgsqlDbType.Text
            };
        }

    }
}
