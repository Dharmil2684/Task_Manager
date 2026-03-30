using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManager.Core.DTOs.Tasks;
using TaskManager.Core.Entities;

namespace TaskManager.Core.Interfaces
{
    public interface ITaskService
    {
        Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, Guid tenantId);
        Task<IEnumerable<TaskResponse>> GetTasksByTenantAsync(Guid tenantId);
        Task<IEnumerable<TaskResponse>> GetMyTasksAsync(Guid tenantId, Guid userId);
        Task<TaskResponse> UpdateTaskStatusAsync(Guid taskId, Guid tenantId, Guid userId, WorkTaskStatus status);
        Task DeleteTaskAsync(Guid taskId, Guid tenantId, Guid userId, UserRole userRole);
    }
}
