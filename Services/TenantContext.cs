using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using TaskManager.Interfaces;

namespace TaskManager.Services
{
    public class TenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        public string CurrentTenantId
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;

                // If there is no user, or they aren't logged in, we return a default/empty state.
                // (Endpoints like /login or /register obviously won't have a TenantId yet)
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return string.Empty;
                }

                var tenantClaim = user.Claims.FirstOrDefault(c => c.Type == "TenantId");

                if (tenantClaim == null || string.IsNullOrWhiteSpace(tenantClaim.Value))
                {
                    throw new UnauthorizedAccessException("User token does not contain a valid Tenant ID.");
                }

                return tenantClaim.Value;
            }
        }
    }
}