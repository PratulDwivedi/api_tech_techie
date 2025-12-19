using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using TechTechie.PostgresRepository.Dynamic.Entities;
using TechTechie.Services.Users.Models;

namespace TechTechie.PostgresRepository
{
    public class NpgSqlDbHelper
    {
        public static string GetCurrentContextSettings(int? tenant_id, int? user_id, int? caller_id)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"SET LOCAL app.tenant_id = '{tenant_id}';");
            sb.AppendLine($"SET LOCAL app.user_id = '{user_id}';");
            sb.AppendLine($"SET LOCAL app.caller_id = '{caller_id}';");

            return sb.ToString();
        }

        public static async Task<List<Dictionary<string, object?>>> GetDynamicDataAsync(NpgsqlConnection connection, string dynamicSql, int tenant_id, int user_id, int caller_id)
        {
            var sb = new StringBuilder();

            sb.Append(GetCurrentContextSettings(tenant_id, user_id, caller_id));

            // Append the function call
            sb.AppendLine($"SELECT dynamic_query(@Sql) ");

            var wasClosed = connection.State == ConnectionState.Closed;
            if (wasClosed)
                await connection.OpenAsync();

            await using var cmd = new NpgsqlCommand(sb.ToString(), connection);
            cmd.Parameters.AddWithValue("@Sql", dynamicSql);

            var resultJson = await cmd.ExecuteScalarAsync() as string;

            if (wasClosed)
                await connection.CloseAsync();

            if (string.IsNullOrWhiteSpace(resultJson))
                return new List<Dictionary<string, object?>>();

            var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object?>>>(resultJson);

            var cleaned = dict?
                .Select(d => d
                    .Where(kv => kv.Value is not null)
                    .ToDictionary(kv => kv.Key, kv => kv.Value))
                .ToList();

            return cleaned ?? new List<Dictionary<string, object?>>();
        }




        public static string GetFunctionParams(Dictionary<string, object> functionParams)
        {
            return string.Join(", ", functionParams.Keys.Select(k => $"{k} := @{k}"));
        }

        public async Task<List<FunctionParamEntity>> GetFunctionParametersAsync(NpgsqlConnection connection, string function_name)
        {
            // function_name or procedure_name both are having same behavior

            string schemaName = "public";
            string functionName = string.Empty;

            if (function_name.Split(".").Length == 2)
            {
                schemaName = function_name.Split(".")[0];
                functionName = function_name.Split(".")[1];
            }
            else
            {
                functionName = function_name;
            }

            const string sql = @"
                        SELECT
                            unnest(p.proargnames) AS param_name,
                            pg_catalog.format_type(unnest(p.proargtypes), NULL) AS param_type,
                            p.proargdefaults IS NOT NULL AS has_defaults,
                            pg_catalog.format_type(p.prorettype, NULL) AS return_type
                        FROM pg_proc p
                            JOIN pg_namespace n ON n.oid = p.pronamespace
                        WHERE p.proname = @FunctionName
                          AND n.nspname = @SchemaName;";

            var parameters = new List<FunctionParamEntity>();

            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@FunctionName", functionName);
            cmd.Parameters.AddWithValue("@SchemaName", schemaName);

            var wasClosed = connection.State == ConnectionState.Closed;
            if (wasClosed)
                await connection.OpenAsync();

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                parameters.Add(new FunctionParamEntity
                {
                    Name = reader.GetString(reader.GetOrdinal("param_name")).Trim(),
                    DataType = reader.GetString(reader.GetOrdinal("param_type")).Trim(),
                    FunctionReturnType = reader.GetString(reader.GetOrdinal("return_type")).Trim(),
                    HasDefault = reader.GetBoolean(reader.GetOrdinal("has_defaults"))
                });
            }

            if (wasClosed)
                await connection.CloseAsync(); // Respect external connection ownership

            return parameters;
        }

        public static string GetFunctionCallText(
    int caller_id,
    string functionName,
    List<FunctionParamEntity> functionParams,
    Dictionary<string, object> data,
    SignedUser signedUser)
        {
            var sb = new StringBuilder();

            // Append current context settings
            sb.Append(GetCurrentContextSettings(signedUser.tenant_id, signedUser.uid, caller_id));

            if (functionName.Contains("("))
            {
                sb.Append($"SELECT {functionName}");
            }
            else
            {
                sb.Append($"SELECT {functionName}(");
                var formattedParams = new List<string>();

                foreach (var param in functionParams)
                {
                    var jsonKeyName = Regex.Replace(param.Name, @"\bp_(\w+)", "$1");

                    // Handle special params
                    if (param.Name is "tenant_id" or "p_tenant_id")
                    {
                        formattedParams.Add($"{param.Name} := {signedUser.tenant_id}");
                        continue;
                    }

                    if (param.Name is "user_id" or "p_user_id")
                    {
                        formattedParams.Add($"{param.Name} := {signedUser.uid}");
                        continue;
                    }

                    if (param.Name is "caller_id" or "p_caller_id")
                    {
                        formattedParams.Add($"{param.Name} := {caller_id}");
                        continue;
                    }

                    // Check if value exists
                    if (!data.TryGetValue(jsonKeyName, out var value) || value is null)
                    {
                        if (!param.HasDefault)
                            formattedParams.Add($"{param.Name} := NULL");
                        continue;
                    }

                    string formattedValue;

                    // Type handling
                    switch (param.DataType)
                    {
                        case "integer":
                            if (int.TryParse(value.ToString(), out var parsedInt))
                                formattedValue = parsedInt.ToString();
                            else
                                formattedValue = "NULL";
                            break;

                        case "boolean":
                            if (bool.TryParse(value.ToString(), out var parsedBool))
                                formattedValue = parsedBool.ToString().ToLower();
                            else
                                formattedValue = "NULL";
                            break;

                        case "text":
                            formattedValue = $"'{value.ToString().Replace("'", "''")}'";
                            break;

                        case "uuid":
                            if (Guid.TryParse(value.ToString(), out var parsedGuid))
                                formattedValue = $"'{parsedGuid}'::uuid";
                            else
                                formattedValue = "NULL";
                            break;

                        default:
                            // Handle generic cases including JSON
                            switch (value)
                            {
                                case string s:
                                    formattedValue = $"'{s.Replace("'", "''")}'";
                                    break;

                                case bool b:
                                    formattedValue = b.ToString().ToLower();
                                    break;

                                case int or long or float or double or decimal:
                                    formattedValue = value.ToString();
                                    break;

                                case JToken jtoken:
                                    formattedValue = FormatJTokenValue(jtoken);
                                    break;

                                case IEnumerable<object> list:
                                    formattedValue = $"'{JsonConvert.SerializeObject(list).Replace("'", "''")}'::jsonb";
                                    break;

                                default:
                                    formattedValue = $"'{JsonConvert.SerializeObject(value).Replace("'", "''")}'::jsonb";
                                    break;
                            }
                            break;
                    }

                    formattedParams.Add($"{param.Name} := {formattedValue}");
                }

                sb.Append(string.Join(", ", formattedParams));
                sb.Append(");");
            }

            return sb.ToString();
        }

        private static string FormatJTokenValue(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.String:
                    return $"'{token.ToString().Replace("'", "''")}'";
                case JTokenType.Integer:
                case JTokenType.Float:
                    return token.ToString();
                case JTokenType.Boolean:
                    return token.ToString().ToLower();
                case JTokenType.Object:
                case JTokenType.Array:
                    return $"'{token.ToString(Formatting.None).Replace("'", "''")}'::jsonb";
                case JTokenType.Null:
                    return "NULL";
                default:
                    return $"'{token.ToString(Formatting.None).Replace("'", "''")}'::jsonb";
            }
        }


        public async Task<List<Dictionary<string, object>>> ExecuteRawSqlAsync(NpgsqlConnection connection, string sql, DynamicParameters? parameters = null)
        {
            var result = new List<Dictionary<string, object>>();

            var wasClosed = connection.State == ConnectionState.Closed;
            if (wasClosed)
                await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);

            if (parameters != null)
            {
                foreach (var paramName in parameters.ParameterNames)
                {
                    var value = parameters.Get<object>(paramName);
                    command.Parameters.AddWithValue(paramName, value ?? DBNull.Value);
                }
            }

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>(reader.FieldCount);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[reader.GetName(i)] = value;
                }
                result.Add(row);
            }

            if (wasClosed)
                await connection.CloseAsync();

            return result;
        }

    }
}
