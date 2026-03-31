using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskManager.Core.DTOs.Auth;
using TaskManager.Core.Interfaces;

namespace TaskManager.API.Controllers
{
    /// <summary>
    /// Handles tenant registration, user login, token refresh, and token revocation.
    /// </summary>
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>Register a new tenant and its admin user.</summary>
        /// <response code="201">Tenant created successfully — returns access + refresh tokens.</response>
        /// <response code="400">Validation error (missing fields, duplicate email).</response>
        [HttpPost("register")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var response = await _authService.RegisterTenantAsync(request);
            return StatusCode(StatusCodes.Status201Created, response);
        }

        /// <summary>Authenticate with email + password.</summary>
        /// <response code="200">Returns access + refresh tokens.</response>
        /// <response code="401">Invalid credentials.</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }

        /// <summary>Exchange a refresh token for a new access token.</summary>
        /// <response code="200">Returns new access + refresh tokens.</response>
        /// <response code="401">Invalid or expired refresh token.</response>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            var response = await _authService.RefreshTokenAsync(request.RefreshToken);
            return Ok(response);
        }

        /// <summary>Revoke the caller's refresh token (logout).</summary>
        /// <response code="204">Refresh token revoked.</response>
        [HttpPost("revoke")]
        [Authorize]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Revoke()
        {
            var userId = GetUserId();
            var tenantId = GetTenantId();
            
            if (userId == Guid.Empty || tenantId == Guid.Empty)
            {
                return BadRequest("Invalid user or tenant identifier.");
            }

            await _authService.RevokeRefreshTokenAsync(userId, tenantId);
            return NoContent();
        }
    }
}
