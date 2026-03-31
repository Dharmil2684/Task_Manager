using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Core.DTOs.Users;
using TaskManager.Core.Entities;
using TaskManager.Core.Interfaces;

namespace TaskManager.API.Controllers
{
    /// <summary>
    /// Read-only user management endpoints scoped to the caller's tenant.
    /// </summary>
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>List all users in the caller's tenant (Managers and TenantAdmins only).</summary>
        /// <response code="200">List of users (sensitive fields stripped).</response>
        /// <response code="403">Insufficient role.</response>
        [HttpGet]
        [Authorize(Roles = "Manager,TenantAdmin")]
        [ProducesResponseType(typeof(IEnumerable<UserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUsers()
        {
            var tenantId = GetTenantId();
            var callerId = GetUserId();
            var callerRole = GetUserRole();

            var users = await _userService.GetUsersByTenantAsync(tenantId);
            users ??= new List<User>();

            // Filter users based on business rules:
            // Managers see themselves and Employees.
            // TenantAdmins see themselves, Managers, and Employees (no other Admins).
            users = users.Where(u =>
                u.Id == callerId || 
                (callerRole == UserRole.Manager && u.Role == UserRole.Employee) ||
                (callerRole == UserRole.TenantAdmin && (u.Role == UserRole.Manager || u.Role == UserRole.Employee))
            ).ToList();

            var response = users.Select(u => new UserResponse
            {
                Id = u.Id,
                TenantId = u.TenantId,
                Email = u.Email,
                Role = u.Role,
                CreatedAt = u.CreatedAt
            });

            return Ok(response);
        }
    }
}
