using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SystemForCrossDomainIdentityManagement;
using TechTechie.Services.Users.ServiceInterfaces;
using TechTechie.WebApi.Helpers;
using TechTechie.WebApi.Helpers.SCIM;
using TypeScheme = TechTechie.WebApi.Helpers.SCIM.TypeScheme;

namespace TechTechie.WebApi.Controllers
{
    [Authorize(Policy = "ApiPolicy")]
    [Route("api/scim")]
    [ApiController]
    public class ScimController : ControllerBase
    {
        private readonly IUserService _userService;
        public ScimController(IUserService userService)
        {
            _userService = userService;
        }

        private async Task<Dictionary<string, object>> ExecuteScimRequest(string id, string entityName, string operationName, string requestJson)
        {
            try
            {
                var signedUser = HttpHelper.GetSignedUser(HttpContext);
                if (signedUser == null || string.IsNullOrEmpty(signedUser.id))
                {
                    throw new UnauthorizedAccessException("Access token as bearer token is not valid. User is not valid.");
                }

                var Data = await _userService.ExecuteScimRoute(id, entityName, operationName, requestJson, signedUser);

                if (Data == null)
                {
                    throw new Exception($"No Data returned for operation '{operationName}' on entity '{entityName}'.");
                }

                return Data;
            }
            catch (UnauthorizedAccessException uaEx)
            {
                // Log unauthorized exception if needed
                throw; // Re-throw to let controller handle as 401
            }
            catch (Exception ex)
            {
                // Log general exception if needed
                throw new Exception($"Error executing SCIM request. Operation: {operationName}, Entity: {entityName}. Details: {ex.Message}", ex);
            }
        }


        [HttpPost("users")]
        public async Task<IActionResult> ScimCreateUser([FromBody] Core2EnterpriseUser user)
        {
            try
            {
                string requestJson = Newtonsoft.Json.JsonConvert.SerializeObject(user);
                var Data = await ExecuteScimRequest("", "User", "Create", requestJson);
                return Ok(Data);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                return Unauthorized(new { error = uaEx.Message });
            }
            catch (Exception ex)
            {
                // Optionally log ex
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // it calls when validate the token first time
        [HttpGet("users/{userId?}")]
        public async Task<IActionResult> ScimGetUser(string userId)
        {
            try
            {
                var Data = await ExecuteScimRequest(userId, "User", "Get", "");
                return Ok(Data);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                return Unauthorized(new { error = uaEx.Message });
            }
            catch (Exception ex)
            {
                // Optionally log ex
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPatch("users/{userId}")]
        public async Task<IActionResult> ScimPatchUser(string userId, [FromBody] Dictionary<string, object> patchModel)
        {
            try
            {
                string requestJson = Newtonsoft.Json.JsonConvert.SerializeObject(patchModel);
                var Data = await ExecuteScimRequest(userId, "User", "Update", requestJson);
                return Ok(Data);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                return Unauthorized(new { error = uaEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> ScimDeleteUser(string userId)
        {
            try
            {
                var Data = await ExecuteScimRequest(userId, "User", "Delete", "");
                return Ok(Data);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                return Unauthorized(new { error = uaEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("groups")]
        public async Task<IActionResult> ScimCreateGroup([FromBody] Core2Group group)
        {
            try
            {
                string requestJson = Newtonsoft.Json.JsonConvert.SerializeObject(group);
                var Data = await ExecuteScimRequest("", "Group", "Create", requestJson);
                return Ok(Data);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                return Unauthorized(new { error = uaEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("groups/{groupId?}")]
        public async Task<IActionResult> ScimGetGroup(string groupId)
        {
            try
            {
                var Data = await ExecuteScimRequest(groupId, "Group", "Get", "");
                return Ok(Data);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                return Unauthorized(new { error = uaEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPatch("groups/{groupId}")]
        public async Task<IActionResult> ScimPatchGroup(string groupId, [FromBody] Dictionary<string, object> patchModel)
        {
            try
            {
                string requestJson = Newtonsoft.Json.JsonConvert.SerializeObject(patchModel);
                var Data = await ExecuteScimRequest(groupId, "Group", "Update", requestJson);
                return Ok(Data);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                return Unauthorized(new { error = uaEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("groups/{groupId}")]
        public async Task<IActionResult> ScimDeleteGroup(string groupId)
        {
            try
            {
                var Data = await ExecuteScimRequest(groupId, "Group", "Delete", "");
                return Ok(Data);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                return Unauthorized(new { error = uaEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }



        [HttpGet("Schemas")]
        public async Task<IActionResult> GetScimSchema()
        {
            var schema = new Lazy<IReadOnlyCollection<TypeScheme>>(
               () =>
                   new TypeScheme[]
                   {
                        SampleTypeScheme.UserTypeScheme,
                        SampleTypeScheme.GroupTypeScheme,
                        SampleTypeScheme.EnterpriseUserTypeScheme,
                        SampleTypeScheme.ResourceTypesTypeScheme,
                        SampleTypeScheme.SchemaTypeScheme,
                        SampleTypeScheme.ServiceProviderConfigTypeScheme
                   });

            return Ok(schema);
        }


        [HttpGet("Schemas/Groups")]
        public async Task<IActionResult> GetScimSchemaGroups()
        {
            var schema = new Lazy<IReadOnlyCollection<TypeScheme>>(
               () =>
                   new TypeScheme[]
                   {
                        SampleTypeScheme.GroupTypeScheme
                   });

            return Ok(schema);
        }
        [HttpGet("Schemas/Users")]
        public async Task<IActionResult> GetScimSchemaUsers()
        {
            var schema = new Lazy<IReadOnlyCollection<TypeScheme>>(
               () =>
                   new TypeScheme[]
                   {
                        SampleTypeScheme.UserTypeScheme,
                        SampleTypeScheme.EnterpriseUserTypeScheme
                   });

            return Ok(schema);
        }

        [HttpGet("ResourceTypes")]
        public async Task<IActionResult> GetScimResourceTypes()
        {
            var types = new Lazy<IReadOnlyCollection<Core2ResourceType>>(
                 () =>
                     new Core2ResourceType[] { SampleResourceTypes.UserResourceType, SampleResourceTypes.GroupResourceType });
            return Ok(types);
        }

        [HttpGet("serviceConfiguration")]
        [HttpGet("ServiceProviderConfig")]
        public async Task<IActionResult> GetScimServiceConfiguration()
        {
            var schema = new Lazy<IReadOnlyCollection<TypeScheme>>(
               () =>
                   new TypeScheme[]
                   {
                        SampleTypeScheme.SchemaTypeScheme,
                        SampleTypeScheme.ServiceProviderConfigTypeScheme
                   });

            return Ok(schema);
        }

    }
}
