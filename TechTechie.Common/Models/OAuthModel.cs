namespace TechTechie.Common.Models
{
    public class OAuthModel
    {
        public string? GrantType { get; set; } = "client_credentials"; // password , client_credentials , authorization_code , refresh_token, 
        public required string TenantId { get; set; }
        public required string ClientId { get; set; }
        public required string ClientSecret { get; set; }
        public string? Scope { get; set; } = "https://graph.microsoft.com/.default";
        public string? UserId { get; set; }
        public string? Password { get; set; }


    }
}
