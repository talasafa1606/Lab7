using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Lab7.Services
{
    public class KeycloakUserService
    {
        //testtesttest
        private readonly IHttpContextAccessor _httpContextAccessor;

        public KeycloakUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetUserId()
        {
            return _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public string GetUsername()
        {
            return _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value;
        }

        public string GetEmail()
        {
            return _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value;
        }

        public bool IsInRole(string role)
        {
            return _httpContextAccessor.HttpContext?.User.IsInRole(role) ?? false;
        }

        public bool HasPermission(string permission)
        {
            var permissionClaim = "permission";
            return _httpContextAccessor.HttpContext?.User.HasClaim(c => 
                c.Type == permissionClaim && c.Value == permission) ?? false;
        }
    }
}