using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Core.DTOs.Tasks;
using TaskManager.Core.Entities;
using TaskManager.Core.Interfaces;

namespace TaskManager.API.Controllers
{
    /// <summary>
    /// CRUD operations for tasks within the caller's tenant.
    /// </summary>
    [Authorize]
    public class TasksController : BaseApiController
    {
        private readonly ITaskService _taskService;

        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        /// <summary>Create a new task (Managers and TenantAdmins only).</summary>
        /// <response code="201">Task created.</response>
        /// <response code="403">Insufficient role.</response>
        [HttpPost]
        [Authorize(Roles = "Manager,TenantAdmin")]
        [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
        {
            var tenantId = GetTenantId();
            var response = await _taskService.CreateTaskAsync(request, tenantId);
            return StatusCode(StatusCodes.Status201Created, response);
        }

        /// <summary>Get all tasks in the caller's tenant (Managers and TenantAdmins only).</summary>
        [HttpGet]
        [Authorize(Roles = "Manager,TenantAdmin")]
        [ProducesResponseType(typeof(IEnumerable<TaskResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllTasks()
        {
            var tenantId = GetTenantId();
            var tasks = await _taskService.GetTasksByTenantAsync(tenantId);
            return Ok(tasks);
        }

        /// <summary>Get tasks assigned to the current user.</summary>
        [HttpGet("mine")]
        [ProducesResponseType(typeof(IEnumerable<TaskResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyTasks()
        {
            var tenantId = GetTenantId();
            var userId = GetUserId();
            var tasks = await _taskService.GetMyTasksAsync(tenantId, userId);
            return Ok(tasks);
        }

        /// <summary>Update a task's status (assignee only — enforced by the service layer).</summary>
        /// <response code="200">Status updated.</response>
        /// <response code="404">Task not found.</response>
        /// <response code="403">Not the assigned user.</response>
        [HttpPatch("{id:guid}/status")]
        [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTaskStatus(Guid id, [FromBody] UpdateTaskStatusRequest request)
        {
            var tenantId = GetTenantId();
            var userId = GetUserId();
            var response = await _taskService.UpdateTaskStatusAsync(id, tenantId, userId, request.Status);
            return Ok(response);
        }

        /// <summary>Delete a task (Managers or TenantAdmins — enforced by the service layer).</summary>
        /// <response code="204">Task deleted.</response>
        /// <response code="404">Task not found.</response>
        /// <response code="403">Not authorised to delete this task.</response>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Manager,TenantAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteTask(Guid id)
        {
            var tenantId = GetTenantId();
            var userId = GetUserId();
            var userRole = GetUserRole();
            await _taskService.DeleteTaskAsync(id, tenantId, userId, userRole);
            return NoContent();
        }
    }
}
