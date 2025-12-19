using Microsoft.AspNetCore.SignalR;


namespace TechTechie.WebApi.Helpers
{
    public class NotificationHub : Hub
    {
        public Task JoinGroup(string group)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, group);
        }

        public Task LogOutUser(string tenant_code, string user_id)
        {
            return Clients.Group(tenant_code).SendAsync("log_out", tenant_code, user_id);
        }
        public Task Notify(string tenant_code, string api_id, string Data)
        {
            return Clients.Group(tenant_code).SendAsync("notify", api_id, Data);
        }

    }
}
