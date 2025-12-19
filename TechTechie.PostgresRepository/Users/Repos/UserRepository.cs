using Dapper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Npgsql;
using TechTechie.PostgresRepository.Tenants.Repos;
using TechTechie.PostgresRepository.Users.Entities;
using TechTechie.Services.Common.Models;
using TechTechie.Services.Users.Models;
using TechTechie.Services.Users.RepositoryInterfaces;
using static Dapper.SqlMapper;

namespace TechTechie.PostgresRepository.Users.Repos
{
    public class UserRepository : IUserRepository
    {

        private readonly TenantDbHelper _tenantDbHelper;
        private readonly IConfiguration _configuration;
        private readonly string _npgSqlconn;
        private readonly TenantRepository _tenantRepository;

        public UserRepository(TenantDbHelper tenantDbHelper, IConfiguration configuration)
        {
            _tenantDbHelper = tenantDbHelper;
            _configuration = configuration;
            _npgSqlconn = _configuration.GetConnectionString("NpgSqlconn")!;
            _tenantRepository = new TenantRepository(tenantDbHelper, configuration);
        }

        public async Task<SignInResponseModel> EmailSignIn(EmailSignInModel emailSignIn)
        {

            var sql = "SELECT fn_get_user_by_email_password(@tenant_code, @email, @password) AS result";

            await using var masterConn = new NpgsqlConnection(_npgSqlconn);
            await masterConn.OpenAsync();

            var jsonResult = await masterConn.QueryFirstOrDefaultAsync<string>(sql, new
            {
                emailSignIn.tenant_code,
                emailSignIn.email,
                emailSignIn.password
            });

            if (string.IsNullOrWhiteSpace(jsonResult))
                throw new Exception("User not found or invalid credentials.");

            // Deserialize JSON into list of dictionaries
            var entity = JsonConvert.DeserializeObject<UserEntity>(jsonResult);

            if (entity == null)
            {
                throw new Exception("User not found or invalid credentials.");
            }

            return GetUserFromEntity(entity);
        }

        public async Task<Dictionary<string, object>> ExecuteScimRoute(string Id, string EntityName, string OperationName, string RequestJson, SignedUser user)
        {
            using var connection = await _tenantDbHelper.GetTenantConnectionAsync(user);

            var sql = @"SELECT public.fn_execute_scim_route(
                    @p_entity_name,
                    @p_operation_name,
                    @p_request_json::jsonb,
                    @p_uid,
                    @p_tenant_id,
                    @p_id
                );";

            var json = await connection.QuerySingleOrDefaultAsync<string>(sql, new
            {
                p_entity_name = EntityName,
                p_operation_name = OperationName,
                p_request_json = RequestJson,
                p_uid = user.uid,
                p_tenant_id = user.tenant_id,
                p_id = Id
            });

            if (!string.IsNullOrEmpty(json))
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(json)!;
            }

            return new Dictionary<string, object>();
        }


        public async Task<ResponseMessageModel> ForgotPassword(ForgotPasswordModel forgotPassword)
        {
            ResponseMessageModel response = new() { IsSuccess = true, Message = "Success", StatusCode = 200 };

            try
            {
                // fogot password will sent a mail to reset the password
                // using reset_password (page_id : 181) template and route

                var signInResponse = await GetUserByEmailAndTenant(forgotPassword.tenant_code, forgotPassword.email);

                SignedUser signedUser = GetSignedUserFromResponse(signInResponse);

                if (signedUser != null && signedUser.uid != null)
                {
                    // With this null check and conversion:
                    if (signedUser.uid.HasValue)
                    {
                        var log_id = await LogResetPasswordMail(signedUser.tenant_id.Value, signedUser.uid.Value);

                        // send mail if mail log found 
                        var result = await _tenantRepository.SendMail(log_id, signedUser);
                    }
                }

            }
            catch (Exception)
            {

                response.IsSuccess = false;
                response.Message = "User not found or invalid credentials.";
                response.StatusCode = 400;
            }

            return response;
        }

        public async Task<int> LogResetPasswordMail(int tenant_id, int uid)
        {
            var sql = "SELECT fn_log_reset_password_mail(@tenant_id, @user_id) AS result";
            int log_id = 0;

            await using var masterConn = new NpgsqlConnection(_npgSqlconn);
            await masterConn.OpenAsync();

            var jsonResult = await masterConn.QueryFirstOrDefaultAsync<string>(sql, new
            {
                tenant_id = tenant_id,
                user_id = uid
            });
            // Replace this line:

            // Deserialize JSON into list of dictionaries
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonResult);

            if (data != null && data.ContainsKey("log_id"))
            {
                log_id = Convert.ToInt32(data["log_id"]);
            }

            return log_id;
        }

        public async Task<SignInResponseModel> GetUserByEmailAndTenant(string tenant_code, string email)
        {
            var sql = "SELECT fn_get_user_by_email(@tenant_code, @email) AS result";

            await using var masterConn = new NpgsqlConnection(_npgSqlconn);
            await masterConn.OpenAsync();

            var jsonResult = await masterConn.QueryFirstOrDefaultAsync<string>(sql, new
            {
                tenant_code,
                email
            });

            if (string.IsNullOrWhiteSpace(jsonResult))
                throw new Exception("User not found or invalid credentials.");

            // Deserialize JSON into list of dictionaries
            var entity = JsonConvert.DeserializeObject<UserEntity>(jsonResult);

            if (entity == null)
            {
                throw new Exception("User not found or invalid credentials.");
            }

            return GetUserFromEntity(entity);
        }

        public async Task<SignedUser> GetUserByPublicAccessCode(string access_code)
        {

            var sql = "SELECT fn_get_user_by_public_access_code(@access_code) AS result";

            await using var masterConn = new NpgsqlConnection(_npgSqlconn);
            await masterConn.OpenAsync();

            var jsonResult = await masterConn.QueryFirstOrDefaultAsync<string>(sql, new
            {
                access_code
            });

            if (string.IsNullOrWhiteSpace(jsonResult))
                throw new Exception("User not found or invalid credentials.");

            // Deserialize JSON into list of dictionaries
            var signedUser = JsonConvert.DeserializeObject<SignedUser>(jsonResult);

            if (signedUser == null)
            {
                throw new Exception("User not found or invalid credentials.");
            }

            return signedUser;
        }

        public async Task<SignInResponseModel> GetUserFromApiKey(string api_key)
        {
            var sql = "SELECT fn_get_user_by_api_key(@api_key) AS result";

            await using var masterConn = new NpgsqlConnection(_npgSqlconn);
            await masterConn.OpenAsync();

            var jsonResult = await masterConn.QueryFirstOrDefaultAsync<string>(sql, new
            {
                api_key
            });

            if (string.IsNullOrWhiteSpace(jsonResult))
                throw new Exception("User not found or invalid credentials.");

            // Deserialize JSON into list of dictionaries
            var entity = JsonConvert.DeserializeObject<UserEntity>(jsonResult);

            if (entity == null)
            {
                throw new Exception("User not found or invalid credentials.");
            }

            return GetUserFromEntity(entity);
        }

        private SignedUser GetSignedUserFromResponse(SignInResponseModel signInResponse)
        {
            return new SignedUser
            {
                tenant_id = signInResponse.tenant_id,
                tenant_code = signInResponse.tenant_code,
                uid = Convert.ToInt32(signInResponse.uid),
                id = signInResponse.id,
                user_name = signInResponse.name,
                email = signInResponse.email,
            };
        }
        private SignInResponseModel GetUserFromEntity(UserEntity entity)
        {
            SignInResponseModel signInResponseModel = new SignInResponseModel
            {
                tenant_id = entity.tenant_id,
                tenant_code = entity.tenant_code,
                uid = entity.uid.ToString(),
                id = entity.id,
                name = entity.name,
                mobile_no = entity.mobile_no,
                email = entity.email,
                token_expiry_minutes = 200,
                Data = entity.data,
            };

            return signInResponseModel;
        }
    }
}
