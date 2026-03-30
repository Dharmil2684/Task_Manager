using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManager.Core.DTOs.Tasks;
using TaskManager.Core.Entities;
using TaskManager.Core.Interfaces;

namespace TaskManager.Services
{
    public class TaskService : ITaskService
    {
        private readonly ICosmosDbRepository<TaskItem> _taskRepository;

        public TaskService(ICosmosDbRepository<TaskItem> taskRepository)
        {
            _taskRepository = taskRepository;
        }

        public async Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, Guid tenantId)
        {
            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Title = request.Title,
                Description = request.Description,
                AssigneeId = request.AssigneeId,
                DueDate = request.DueDate,
                Status = WorkTaskStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _taskRepository.AddItemAsync(task);
            return MapToResponse(task);
        }

        public async Task DeleteTaskAsync(Guid taskId, Guid tenantId, Guid userId, UserRole userRole)
        {
            // Load the task first to verify it exists and perform authorization
            var task = await _taskRepository.GetItemAsync(taskId.ToString(), tenantId.ToString());
            if (task == null)
            {
                throw new KeyNotFoundException("Task not found.");
            }

            // Only the assignee, Managers, or TenantAdmins can delete
            if (task.AssigneeId != userId && userRole != UserRole.Manager && userRole != UserRole.TenantAdmin)
            {
                throw new UnauthorizedAccessException("You do not have permission to delete this task.");
            }

            await _taskRepository.DeleteItemAsync(taskId.ToString(), tenantId.ToString());
        }

        public async Task<IEnumerable<TaskResponse>> GetMyTasksAsync(Guid tenantId, Guid userId)
        {
            var query = "SELECT * FROM c WHERE c.assigneeId = @userId AND c.tenantId = @tenantId";
            var parameters = new Dictionary<string, object>
            {
                { "@userId", userId },
                { "@tenantId", tenantId }
            };
            
            var tasks = await _taskRepository.GetItemsAsync(query, parameters);
            return tasks.Select(MapToResponse);
        }

        public async Task<IEnumerable<TaskResponse>> GetTasksByTenantAsync(Guid tenantId)
        {
            var query = "SELECT * FROM c WHERE c.tenantId = @tenantId";
            var parameters = new Dictionary<string, object> { { "@tenantId", tenantId } };
            var tasks = await _taskRepository.GetItemsAsync(query, parameters);
            return tasks.Select(MapToResponse);
        }

        public async Task<TaskResponse> UpdateTaskStatusAsync(Guid taskId, Guid tenantId, Guid userId, WorkTaskStatus status)
        {
            var task = await _taskRepository.GetItemAsync(taskId.ToString(), tenantId.ToString());
            if (task == null)
            {
                throw new KeyNotFoundException("Task not found.");
            }

            // Authorization: only the assigned user can update status
            if (task.AssigneeId != userId)
            {
                throw new UnauthorizedAccessException("You are not assigned to this task and cannot update its status.");
            }

            task.Status = status;
            task.UpdatedAt = DateTime.UtcNow;

            await _taskRepository.UpdateItemAsync(taskId.ToString(), task, tenantId.ToString());
            
            return MapToResponse(task);
        }

        private static TaskResponse MapToResponse(TaskItem task)
        {
            return new TaskResponse
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                AssigneeId = task.AssigneeId,
                DueDate = task.DueDate,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt
            };
        }
    }
}
