using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Core.Entities;
using TaskManager.Core.Exceptions;

namespace TaskManager.API.Controllers
{
    /// <summary>
    /// Shared base for all API controllers. Centralises JWT claim extraction
    /// so individual controllers stay focused on orchestration.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        /// <summary>Extract the authenticated user's ID from the JWT.</summary>
        protected Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? throw new AuthorizationException("User ID claim is missing from the token.");
            return Guid.Parse(claim.Value);
        }

        /// <summary>Extract the tenant ID from the JWT.</summary>
        protected Guid GetTenantId()
        {
            var claim = User.FindFirst("tenantId")
                ?? throw new AuthorizationException("Tenant ID claim is missing from the token.");
            return Guid.Parse(claim.Value);
        }

        /// <summary>Extract the user's role from the JWT.</summary>
        protected UserRole GetUserRole()
        {
            var claim = User.FindFirst(ClaimTypes.Role)
                ?? throw new AuthorizationException("Role claim is missing from the token.");
            return Enum.Parse<UserRole>(claim.Value);
        }
    }
}
