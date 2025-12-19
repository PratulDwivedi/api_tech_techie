using Microsoft.SystemForCrossDomainIdentityManagement;

namespace TechTechie.WebApi.Helpers.SCIM
{
    public class ScimUsers
    {
        public List<string> schemas { get; set; } = new List<string> { "urn:ietf:params:scim:api:messages:2.0:ListResponse" };
        public int totalResults { get; set; }
        public List<Core2EnterpriseUser> Resources { get; set; } = new List<Core2EnterpriseUser>();
        public int startIndex { get; set; } = 1;
        public int itemsPerPage { get; set; }
    }

}
