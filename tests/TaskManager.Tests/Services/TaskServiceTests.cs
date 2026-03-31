using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using TaskManager.Core.DTOs.Tasks;
using TaskManager.Core.Entities;
using TaskManager.Core.Interfaces;
using TaskManager.Services;
using Xunit;

namespace TaskManager.Tests.Services
{
    public class TaskServiceTests
    {
        private readonly Mock<ICosmosDbRepository<TaskItem>> _mockTaskRepo;
        private readonly TaskService _taskService;

        public TaskServiceTests()
        {
            _mockTaskRepo = new Mock<ICosmosDbRepository<TaskItem>>();
            _taskService = new TaskService(_mockTaskRepo.Object);
        }

        [Fact]
        public async Task DeleteTaskAsync_Employee_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            
            var task = new TaskItem { Id = taskId, TenantId = tenantId, AssigneeId = userId };
            _mockTaskRepo.Setup(x => x.GetItemAsync(taskId.ToString(), tenantId.ToString()))
                .ReturnsAsync(task);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _taskService.DeleteTaskAsync(taskId, tenantId, userId, UserRole.Employee));
        }

        [Theory]
        [InlineData(UserRole.Manager)]
        [InlineData(UserRole.TenantAdmin)]
        public async Task DeleteTaskAsync_AuthorizedRoles_DeletesSuccessfully(UserRole role)
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            
            var task = new TaskItem { Id = taskId, TenantId = tenantId, AssigneeId = userId };
            _mockTaskRepo.Setup(x => x.GetItemAsync(taskId.ToString(), tenantId.ToString()))
                .ReturnsAsync(task);

            // Act
            await _taskService.DeleteTaskAsync(taskId, tenantId, userId, role);

            // Assert
            _mockTaskRepo.Verify(x => x.DeleteItemAsync(taskId.ToString(), tenantId.ToString()), Times.Once);
        }

        [Fact]
        public async Task DeleteTaskAsync_TaskNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            
            _mockTaskRepo.Setup(x => x.GetItemAsync(taskId.ToString(), tenantId.ToString()))
                .ReturnsAsync((TaskItem)null!);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _taskService.DeleteTaskAsync(taskId, tenantId, userId, UserRole.Manager));
        }

        [Fact]
        public async Task CreateTaskAsync_ValidRequest_ReturnsTaskResponse()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var request = new CreateTaskRequest
            {
                Title = "Test Task",
                Description = "Description",
                AssigneeId = Guid.NewGuid(),
                DueDate = DateTime.UtcNow.AddDays(1)
            };

            _mockTaskRepo.Setup(x => x.AddItemAsync(It.IsAny<TaskItem>(), tenantId.ToString()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _taskService.CreateTaskAsync(request, tenantId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Title, result.Title);
            Assert.Equal(WorkTaskStatus.Pending, result.Status);
            _mockTaskRepo.Verify(x => x.AddItemAsync(It.IsAny<TaskItem>(), tenantId.ToString()), Times.Once);
        }
    }
}
