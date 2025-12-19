
using Dapper;
using Newtonsoft.Json;
using Npgsql;
using System.Data;
using TechTechie.PostgresRepository.Dynamic.Entities;
using TechTechie.Services.Common.Models;
using TechTechie.Services.Dynamic.RepositoryInterfaces;
using TechTechie.Services.Users.Models;


namespace TechTechie.PostgresRepository.Dynamic.Repos
{
    public class DynamicRepository : IDynamicRepository
    {

        private readonly TenantDbHelper _tenantDbHelper;

        public DynamicRepository(TenantDbHelper tenantDbHelper)
        {
            _tenantDbHelper = tenantDbHelper;
        }


        public async Task<ResponseMessageModel> ExecuteRoute(RequestMessageModel requestMessage, SignedUser signedUser)
        {
            using var connection = await _tenantDbHelper.GetTenantConnectionAsync(signedUser);

            var pageSchema = await GetPageSchema(connection, requestMessage.RouteName);

            ResponseMessageModel response = new() { IsSuccess = true, Message = "Success", StatusCode = 200 };
            int caller_id = pageSchema.id;
            string sqlFunCallText = string.Empty;
            DateTime ProcessStartOn = DateTime.UtcNow;
            int RecordCount = 0;
            string functionName = string.Empty;

            try
            {
                if (caller_id == 0)
                {
                    // Calling the function directly from the route
                    functionName = requestMessage.RouteName;

                    if (!string.IsNullOrWhiteSpace(functionName) && !functionName.Contains("."))
                    {
                        functionName = $"public.{functionName}";
                    }
                }
                else
                {
                    functionName = GetFunctionName(requestMessage, pageSchema);
                }

                var functionParams = await new NpgSqlDbHelper().GetFunctionParametersAsync(connection, functionName);

                sqlFunCallText = NpgSqlDbHelper.GetFunctionCallText(caller_id, functionName, functionParams, requestMessage.Data!, signedUser);

                string functionnReturnType = "";

                if (functionParams.Count > 0)
                {
                    functionnReturnType = functionParams[0].FunctionReturnType;
                }

                if (functionnReturnType == "jsonb" || functionnReturnType == "")
                {
                    var resultJson = await connection.QuerySingleOrDefaultAsync<string>(sqlFunCallText);

                    if (!string.IsNullOrWhiteSpace(resultJson))
                    {
                        // Check if it starts with {, meaning it's a single object and needs wrapping
                        if (resultJson.TrimStart().StartsWith("{"))
                        {
                            resultJson = $"[{resultJson}]";
                        }

                        var list = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(resultJson);

                        // Remove null values manually
                        var cleaned = list?
                            .Select(dict => dict
                                .Where(kv => kv.Value != null)
                                .ToDictionary(kv => kv.Key, kv => kv.Value))
                            .ToList();

                        response.Data = cleaned;
                    }

                }
                else
                {
                    throw new Exception("Function return type must be jsonb.");

                    //var result = await new NpgSqlDbHelper(_config).ExecuteRawSqlAsync(_dbConnection, sqlFunCallText);
                    //return result;

                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 400;
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            var log_id = await SaveApiLogAsync(requestMessage, caller_id, sqlFunCallText,
                requestMessage.RouteName, response, RecordCount,
                ProcessStartOn, signedUser);

            return response;
        }


        private async Task<PageEntity> GetPageSchema(NpgsqlConnection connection, string RouteName)
        {
            PageEntity pageEntity = new PageEntity();

            var sql = "SELECT public.fn_get_page_schema(@RouteName)::text";

            var json = await connection.QuerySingleOrDefaultAsync<string>(sql, new { RouteName });

            if (!string.IsNullOrEmpty(json))
            {
                pageEntity = JsonConvert.DeserializeObject<PageEntity>(json)!;
            }

            return pageEntity;
        }

        private string GetFunctionName(RequestMessageModel requestMessage, PageEntity pageEntity)
        {

            string? functionName = null;

            if (requestMessage.Data != null)
            {
                switch (requestMessage.HttpMethod.ToLowerInvariant())
                {
                    case "get":
                        functionName = pageEntity.binding_name_get;
                        break;
                    case "post":
                        functionName = pageEntity.binding_name_post;
                        break;
                    case "delete":
                        functionName = pageEntity.binding_name_delete;
                        break;
                }
            }
            if (string.IsNullOrEmpty(functionName))
                throw new Exception("Function is not mapped with route including schema");

            return functionName;
        }


        public async Task<int> SaveApiLogAsync(
            RequestMessageModel requestMessage,
            int CallerId,
            string CalledFunction,
            string RouteName,
            ResponseMessageModel response,
            int RecordCount, DateTime ProcessStartOn, SignedUser signedUser)
        {

            int log_id = 0;
            string signedUserJson = JsonConvert.SerializeObject(signedUser);
            string requestMessageJson = JsonConvert.SerializeObject(requestMessage);
            string responseJson = JsonConvert.SerializeObject(response);
            try
            {
                var result = await _tenantDbHelper.ExecuteTenantFunction(signedUser, CallerId,
            "SELECT fn_save_api_log(@p_caller_id, @p_RouteName, @p_called_function, @p_signed_user::jsonb," +
            " @p_request::jsonb, @p_response::jsonb, @p_IsSuccess, @p_response_Message, @p_record_count, " +
            " @p_process_start_on::timestamp, @p_tenant_id, @p_created_by, @p_created_at::timestamp)",
                new
                {
                    p_caller_id = CallerId,
                    p_RouteName = RouteName,
                    p_called_function = CalledFunction,
                    p_signed_user = signedUserJson,
                    p_request = requestMessageJson,
                    p_response = responseJson,
                    p_IsSuccess = response.IsSuccess,
                    p_response_Message = response.Message,
                    p_record_count = RecordCount,
                    p_process_start_on = ProcessStartOn,
                    p_tenant_id = signedUser.tenant_id,
                    p_created_by = signedUser.uid,
                    p_created_at = DateTime.UtcNow
                });

                // Parse the returned JSON to extract the log_id
                var jsonDoc = JsonConvert.DeserializeObject<Dictionary<string, object>>(result);
                log_id = Convert.ToInt32(jsonDoc["log_id"]);

            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
            }
            return log_id;
        }

        public async Task<string> GetRouteName(int page_id, int control_id, SignedUser signedUser)
        {
            using var connection = await _tenantDbHelper.GetTenantConnectionAsync(signedUser);
            string RouteName = "";
            if (control_id > 0)
            {
                var sql = "select RouteName from pages where is_active = true and id in (select binding_list_page_id from section_controls where id = @control_id)";

                RouteName = await connection.QuerySingleOrDefaultAsync<string>(sql, new { control_id });
            }
            if (page_id > 0)
            {
                var sql = "select RouteName from pages where is_active = true and id=@page_id";

                RouteName = await connection.QuerySingleOrDefaultAsync<string>(sql, new { page_id });
            }


            if (string.IsNullOrEmpty(RouteName))
            {
                throw new Exception("Page not found or route name is empty.");
            }

            return RouteName;
        }

        public async Task<TemplateModel> GetTemplateAsyc(int id, SignedUser signedUser)
        {
            using var connection = await _tenantDbHelper.GetTenantConnectionAsync(signedUser);

            var sql = "select fn_get_template_to_render(@id, @tenant_id)";

            var jsosString = await connection.QuerySingleOrDefaultAsync<string>(sql, new { id, signedUser.tenant_id });

            if (string.IsNullOrWhiteSpace(jsosString))
            {
                throw new Exception("Template not found or empty.");
            }

            var template = JsonConvert.DeserializeObject<TemplateModel>(jsosString);
            if (template == null)
            {
                throw new Exception("Failed to deserialize template.");
            }

            return template;
        }
    }

}