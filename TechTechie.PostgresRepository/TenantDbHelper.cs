
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Text;
using TechTechie.Services.Users.Models;

namespace TechTechie.PostgresRepository
{
    public class TenantDbHelper
    {
        private readonly IConfiguration _configuration;

        public TenantDbHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<NpgsqlConnection> GetTenantConnectionAsync(SignedUser signedUser)
        {
            var NpgSqlconn = _configuration.GetConnectionString("NpgSqlconn");

            await using var masterConn = new NpgsqlConnection(NpgSqlconn);
            await masterConn.OpenAsync();

            const string sql = "SELECT connection_string FROM tenants WHERE id = @tenant_id LIMIT 1";

            await using var cmd = new NpgsqlCommand(sql, masterConn);
            cmd.Parameters.AddWithValue("@tenant_id", signedUser.tenant_id!);

            var tenantConnectionString = await cmd.ExecuteScalarAsync() as string;

            if (string.IsNullOrWhiteSpace(tenantConnectionString))
                tenantConnectionString = NpgSqlconn;

            var conn_tenant = new NpgsqlConnection(tenantConnectionString);
            // await conn_tenant.OpenAsync();

            return conn_tenant;

        }

        public async Task<string> ExecuteTenantFunction(SignedUser signedUser, int caller_id, string functionCallString, object? param)
        {
            var conn_tenant = await GetTenantConnectionAsync(signedUser);

            var sb = new StringBuilder();

            sb.AppendLine($"SET LOCAL app.tenant_id = '{signedUser.tenant_id}';");
            sb.AppendLine($"SET LOCAL app.user_id = '{signedUser.uid}';");
            sb.AppendLine($"SET LOCAL app.caller_id = '{caller_id}';");
            sb.AppendLine(functionCallString);

            string sqlScript = sb.ToString();

            var jsonResult = await conn_tenant.QuerySingleOrDefaultAsync<string>(
                     sqlScript, param);

            return jsonResult!;

        }
    }

}
