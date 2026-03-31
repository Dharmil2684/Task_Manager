using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskManager.Core.DTOs.Auth;
using TaskManager.Core.Interfaces;

namespace TaskManager.API.Controllers
{
    /// <summary>
    /// Handles user invitations — create invitations (admin-only) and accept them (anonymous).
    /// </summary>
    public class InvitationsController : BaseApiController
    {
        private readonly IInvitationService _invitationService;

        public InvitationsController(IInvitationService invitationService)
        {
            _invitationService = invitationService;
        }

        /// <summary>Send an invitation email to a new user (TenantAdmin only).</summary>
        /// <response code="204">Invitation sent.</response>
        /// <response code="403">Only TenantAdmins can send invitations.</response>
        [HttpPost]
        [Authorize(Roles = "TenantAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateInvitation([FromBody] InviteUserRequest request)
        {
            var tenantId = GetTenantId();
            await _invitationService.CreateInvitationAsync(request, tenantId);
            return NoContent();
        }

        /// <summary>Accept an invitation and create the user account.</summary>
        /// <response code="204">Account created successfully.</response>
        /// <response code="400">Invalid or expired token.</response>
        [HttpPost("accept")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationRequest request)
        {
            await _invitationService.AcceptInvitationAsync(request.Token, request.Password);
            return NoContent();
        }
    }
}
